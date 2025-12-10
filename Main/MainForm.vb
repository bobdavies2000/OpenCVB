Imports System.IO
Imports System.Text.RegularExpressions
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
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
            Try
                Dim algListPath = Path.Combine(CurDir(), "Data", "AvailableAlgorithms.txt")
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
            If myTask Is Nothing Then Exit Sub
            'If myTask IsNot Nothing Then  if mytask.sharpgl IsNot Nothing Then sharpGL.Activate()
            If myTask IsNot Nothing Then If myTask.treeView IsNot Nothing Then myTask.treeView.Activate()
            If myTask IsNot Nothing Then If myTask.allOptions IsNot Nothing Then myTask.allOptions.Activate()
        End Sub
        Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            SaveSettings()
            myTask = Nothing
            StopCamera()
        End Sub
        Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            isPlaying = Not isPlaying

            ' Load and set the appropriate image
            Try
                ' Dispose old image if it exists
                If PausePlayButton.Image IsNot Nothing Then
                    PausePlayButton.Image.Dispose()
                End If

                If isPlaying Then
                    Dim pausePath = Path.Combine(homeDir + "\Main\Data", "PauseButton.png")
                    If File.Exists(pausePath) Then
                        PausePlayButton.Image = New Bitmap(pausePath)
                    End If
                Else
                    Dim playPath = Path.Combine(homeDir + "\Main\Data", "Run.png")
                    If File.Exists(playPath) Then
                        PausePlayButton.Image = New Bitmap(playPath)
                    End If
                End If

                ' Force the button to refresh
                PausePlayButton.Invalidate()
            Catch ex As Exception
                Debug.WriteLine("Error loading button image: " + ex.Message)
            End Try

            myTask = New cvbTask(camImages, settings)
            myTask.colorizer = New DepthColorizer_Basics

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
            fpsTimer.Enabled = True
        End Sub
        Private Sub TreeViewTimer_Tick(sender As Object, e As EventArgs) Handles TreeViewTimer.Tick
            If myTask.treeView IsNot Nothing Then myTask.treeView.Timer2_Tick(sender, e)
        End Sub
    End Class











    Partial Public Class MainForm
        Dim DrawingRectangle As Boolean
        Dim drawRect As New cv.Rect
        Dim LastX As Integer
        Dim LastY As Integer
        Dim mouseClickFlag As Boolean
        Dim mousePicTag As Integer
        Dim mouseDownPoint As cv.Point
        Dim mouseMovePoint As cv.Point ' last place the mouse was located in any of the OpenCVB images.
        Dim activeMouseDown As Boolean
        Dim BothFirstAndLastReady As Boolean
        Private Sub CamPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseUp, campicPointCloud.MouseUp, campicLeft.MouseUp, campicRight.MouseUp
            Try
                If DrawingRectangle Then DrawingRectangle = False
                activeMouseDown = False
            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseUp: " + ex.Message)
            End Try
        End Sub
        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseDown, campicPointCloud.MouseDown, campicLeft.MouseDown, campicRight.MouseDown
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            Try
                Dim pic = DirectCast(sender, PictureBox)
                If e.Button = System.Windows.Forms.MouseButtons.Right Then
                    activeMouseDown = True
                End If
                If e.Button = System.Windows.Forms.MouseButtons.Left Then
                    DrawingRectangle = True
                    BothFirstAndLastReady = False ' we have to see some movement after mousedown.
                    drawRect.Width = 0
                    drawRect.Height = 0
                    mouseDownPoint.X = x
                    mouseDownPoint.Y = y
                End If
            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseDown: " + ex.Message)
            End Try
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles campicRGB.MouseMove, campicPointCloud.MouseMove, campicLeft.MouseMove, campicRight.MouseMove
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            Try
                Dim pic = DirectCast(sender, PictureBox)
                mousePicTag = pic.Tag
                If activeMouseDown Then Exit Sub
                If DrawingRectangle Then
                    mouseMovePoint.X = x
                    mouseMovePoint.Y = y
                    If mouseMovePoint.X < 0 Then mouseMovePoint.X = 0
                    If mouseMovePoint.Y < 0 Then mouseMovePoint.Y = 0
                    drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                    drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                    drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                    drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                    If drawRect.X + drawRect.Width > campicRGB.Width Then drawRect.Width = campicRGB.Width - drawRect.X
                    If drawRect.Y + drawRect.Height > campicRGB.
                        Height Then drawRect.Height = campicRGB.Height - drawRect.Y
                    BothFirstAndLastReady = True
                End If

            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseMove: " + ex.Message)
            End Try

            StatusLabel.Text = String.Format("X: {0}, Y: {1}    ", x, y)
            StatusLabel.Text += String.Format("Last click: {0}, {1}    ", myTask.clickPoint.X, myTask.clickPoint.Y)

            If drawRect.Width > 0 And drawRect.Height > 0 Then
                StatusLabel.Text += "DrawRect = " + String.Format("x: {0}, y: {1}, w: {2}, h: {3}", drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
            End If
        End Sub
        Private Sub PictureBox_MouseClick(sender As Object, e As MouseEventArgs) Handles campicRGB.MouseClick, campicPointCloud.MouseClick, campicLeft.MouseClick, campicRight.MouseClick
            Dim picBox = TryCast(sender, PictureBox)
            Dim x As Integer = e.X * settings.workRes.Width / campicRGB.Width
            Dim y As Integer = e.Y * settings.workRes.Height / campicRGB.Height
            myTask.clickPoint = New cv.Point(x, y)
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs) Handles campicRGB.DoubleClick, campicPointCloud.DoubleClick, campicLeft.DoubleClick, campicRight.DoubleClick
            DrawingRectangle = False
        End Sub
    End Class







    Partial Public Class MainForm
        Dim camera As GenericCamera = Nothing
        Dim cameraRunning As Boolean = False
        Dim dstImages As CameraImages.images
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
        Private Sub StartUpTimer_Tick(sender As Object, e As EventArgs) Handles StartUpTimer.Tick
            StartUpTimer.Enabled = False
            PausePlayButton.PerformClick()
            fpsTimer.Enabled = True
        End Sub
        Private Sub CamSwitchTimer_Tick(sender As Object, e As EventArgs) Handles CamSwitchTimer.Tick
            Me.Refresh()
        End Sub
        Private Sub StartCamera()
            If camera Is Nothing AndAlso settings IsNot Nothing Then
                Try
                    ' Select camera based on settings.cameraName
                    Select Case settings.cameraName
                        Case "StereoLabs ZED 2/2i"
                            camera = New Camera_ZED2(settings.workRes, settings.captureRes, settings.cameraName)
                        Case "Intel(R) RealSense(TM) Depth Camera 435i", "Intel(R) RealSense(TM) Depth Camera 455"
                            camera = New Camera_RS2(settings.workRes, settings.captureRes, settings.cameraName)
                        Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                            camera = New Camera_ORB(settings.workRes, settings.captureRes, settings.cameraName)
                        Case Else
                            ' Default to ZED if camera name not recognized
                            camera = New Camera_ZED2(settings.workRes, settings.captureRes, "StereoLabs ZED 2/2i")
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
        Private Sub StopCamera()
            cameraRunning = False
            If camera IsNot Nothing Then
                ' Unsubscribe from event
                RemoveHandler camera.FrameReady, AddressOf Camera_FrameReady
                camera.childStopCamera()
                camera = Nothing
            End If
        End Sub
        Private Sub Camera_FrameReady(sender As GenericCamera)
            ' This event is raised from the background thread, so we need to marshal to UI thread
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(Of GenericCamera)(AddressOf Camera_FrameReady), sender)
                Return
            End If

            ' Now we're on the UI thread, safe to access UI elements
            If Not cameraRunning OrElse camera Is Nothing Then Return
            Try
                If camImages Is Nothing Then camImages = New CameraImages.images(settings.workRes)
                For i = 0 To camImages.images.Count - 1
                    camImages.images(i) = sender.camImages.images(i)
                Next
                processImages(camImages)
            Catch ex As Exception
                Debug.WriteLine("Camera_FrameReady error: " + ex.Message)
            End Try
        End Sub
        Private Sub UpdatePictureBox(picBox As PictureBox, image As cv.Mat)
            If image IsNot Nothing AndAlso image.Width > 0 Then
                Dim displayImage = image.Clone()
                If drawRect.Width > 0 And drawRect.Height > 0 Then
                    displayImage.Rectangle(drawRect, cv.Scalar.White, 1)
                End If

                displayImage = displayImage.Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
                Dim bitmap = cvext.BitmapConverter.ToBitmap(displayImage)
                If picBox.Image IsNot Nothing Then picBox.Image.Dispose()
                picBox.Image = bitmap
                displayImage.Dispose()
            End If
        End Sub
        Private Sub campicRGB_Paint(sender As Object, e As PaintEventArgs) Handles campicRGB.Paint
            If camera Is Nothing Then Exit Sub
            If myTask Is Nothing Then Exit Sub
            If CameraSwitching.Visible Then
                If camera.cameraFrameCount > 0 Then
                    CameraSwitching.Visible = False
                    CamSwitchTimer.Enabled = False
                End If
            End If

            Try
                For i = 0 To myTask.dst.Count - 1
                    UpdatePictureBox(pics(i), myTask.dst(i))
                Next
            Catch ex As Exception
                Debug.WriteLine("Camera display error: " + ex.Message)
            End Try
        End Sub
    End Class






    Partial Public Class MainForm
        Private Sub processImages(camImages As CameraImages.images)
            ' process the images and put the results in dst().
            myTask.color = camImages.images(0)
            myTask.pointCloud = camImages.images(1)
            myTask.leftView = camImages.images(2)
            myTask.rightView = camImages.images(3)

            myTask.pcSplit = myTask.pointCloud.Split()
            myTask.colorizer.Run(myTask.pcSplit(2))

            myTask.dst = {myTask.color, myTask.depthRGB, myTask.leftView, myTask.rightView}

            AlgDescription.Text = myTask.desc
        End Sub
    End Class




End Namespace
