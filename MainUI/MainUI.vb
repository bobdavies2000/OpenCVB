Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Runtime.Intrinsics
Imports System.Text.RegularExpressions
Imports System.Threading
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Namespace MainApp
    Partial Public Class MainUI : Inherits Form
        Dim isPlaying As Boolean
        Dim homeDir As String = ""
        Public settingsIO As jsonIO
        Dim algHistory As New List(Of String)
        Dim recentMenu() As ToolStripMenuItem
        Dim labels As List(Of Label)
        Dim pics As New List(Of PictureBox)
        Dim resolutionDetails As String
        Dim magnifyIndex As Integer
        Dim windowsFont = New System.Drawing.Font("Tahoma", 9)
        Private Sub addPics()
            pics.Clear()

            For i = 0 To 3
                Dim pic = New PictureBox()
                AddHandler pic.DoubleClick, AddressOf campic_DoubleClick
                AddHandler pic.Click, AddressOf clickPic
                AddHandler pic.Paint, AddressOf Pic_Paint
                AddHandler pic.MouseDown, AddressOf CamPic_MouseDown
                AddHandler pic.MouseUp, AddressOf CamPic_MouseUp
                AddHandler pic.MouseMove, AddressOf CamPic_MouseMove
                pic.Tag = i
                pic.BackColor = Color.Black
                pic.Visible = True
                pic.SizeMode = PictureBoxSizeMode.StretchImage
                pic.Width = settings.displayRes.Width
                pic.Height = settings.displayRes.Height
                Me.Controls.Add(pic)

                pics.Add(pic)
            Next
        End Sub
        Private Sub removePics()
            ' Remove event handlers from PictureBox controls to prevent handle leaks
            For Each pic In pics
                If pic IsNot Nothing Then
                    RemoveHandler pic.DoubleClick, AddressOf campic_DoubleClick
                    RemoveHandler pic.Click, AddressOf clickPic
                    RemoveHandler pic.Paint, AddressOf Pic_Paint
                    RemoveHandler pic.MouseDown, AddressOf CamPic_MouseDown
                    RemoveHandler pic.MouseUp, AddressOf CamPic_MouseUp
                    RemoveHandler pic.MouseMove, AddressOf CamPic_MouseMove
                    ' Dispose the image if it exists
                    If pic.Image IsNot Nothing Then
                        pic.Image.Dispose()
                        pic.Image = Nothing
                    End If
                    Me.Controls.Remove(pic)
                End If
            Next
            pics.Clear()
        End Sub
        Private Sub algHistory_Clicked(sender As Object, e As EventArgs)
            Dim item = TryCast(sender, ToolStripMenuItem)
            settings.algorithm = item.Text
            If AvailableAlgorithms.Items.Contains(settings.algorithm) = False Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedItem = settings.algorithm
            End If
        End Sub
        Public Sub setupAlgorithmHistory()
            If recentMenu Is Nothing Then
                If settings.algorithmHistory.Count > 0 Then ReDim recentMenu(settings.algorithmHistory.Count - 1)
            End If
            For Each alg In settings.algorithmHistory
                algHistory.Add(alg)
                If AvailableAlgorithms.Items.Contains(alg) = False Then Continue For
                RecentList.DropDownItems.Add(alg)
                AddHandler RecentList.DropDownItems(RecentList.DropDownItems.Count - 1).Click, AddressOf algHistory_Clicked
            Next
        End Sub
        Private Sub updateAlgorithmHistory()
            Dim copyList As List(Of String)
            Dim maxHistory As Integer = 50
            'If TestAllTimer.Enabled Then Exit Sub

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
        Public Sub New()
            InitializeComponent()

            Dim executingAssemblyPath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            Dim currentDir = New DirectoryInfo(Path.GetDirectoryName(executingAssemblyPath))
            Dim projectDirectory As String = ""

            ' Navigate up the directory tree to find MainUI.vbproj
            While currentDir IsNot Nothing
                Dim vbprojFile = currentDir.GetFiles("MainUI.vbproj")
                If vbprojFile.Length > 0 Then
                    projectDirectory = currentDir.FullName
                    Exit While
                End If
                currentDir = currentDir.Parent
            End While

            Dim projectDir = New DirectoryInfo(projectDirectory)
            Directory.SetCurrentDirectory(projectDir.FullName + "/../")
            homeDir = Path.GetDirectoryName(projectDir.FullName) + "\"

            labels = New List(Of Label)({labelRGB, labelPointCloud, labelLeft, labelRight})
            For Each lab In labels
                lab.Text = ""
            Next

            settingsIO = New jsonIO(Path.Combine(homeDir, "settings.json"))
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
            MagnifyTimer.Enabled = True
            magnifyIndex += 1
        End Sub
        Private Sub MagnifyTimer_Tick(sender As Object, e As EventArgs) Handles MagnifyTimer.Tick
            Dim ratio = settings.workRes.Width / pics(0).Width
            Dim r = New cv.Rect(task.drawRect.X * ratio, task.drawRect.Y * ratio,
                                task.drawRect.Width * ratio, task.drawRect.Height * ratio)
            r = validateRect(r, task.dstList(task.mousePicTag).Width, task.dstList(task.mousePicTag).Height)
            If r.Width = 0 Or r.Height = 0 Then Exit Sub
            Dim img = task.dstList(task.mousePicTag)(r).Resize(New cv.Size(task.drawRect.Width * 5, task.drawRect.Height * 5))
            cv.Cv2.ImShow("DrawRect Region " + CStr(magnifyIndex), img)
        End Sub
        Private Sub MainForm_Closing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
            If TestAllTimer.Enabled = False Then SaveJsonSettings()
            If isPlaying Then
                vbc.task.Dispose()
                isPlaying = False
                StopCamera()
            End If
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
        Private Sub SaveJsonSettings()
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

            Dim labelHeight As Integer = 20
            Dim topStart As Integer = MainToolStrip.Height
            Dim offset = 10
            Dim h As Integer = (Me.Height - StatusLabel.Height - topStart - labelHeight * 2) / 2 - 20
            Dim w As Integer = Me.Width / 2 - offset * 2
            For i = 0 To pics.Count - 1
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

            settings.displayRes = New cv.Size(w, h)

            resolutionDetails = "CaptureRes " + CStr(settings.captureRes.Width) + "x" + CStr(settings.captureRes.Height) +
                                ", WorkRes " + CStr(settings.workRes.Width) + "x" + CStr(settings.workRes.Height) +
                                ", DisplayRes " + CStr(settings.displayRes.Width) + "x" + CStr(settings.displayRes.Height)
            If task IsNot Nothing Then task.resolutionDetails = resolutionDetails

            StatusLabel.Location = New Point(offset, pics(2).Top + h)
            StatusLabel.Width = w * 2
        End Sub
        Private Sub updatePath(neededDirectory As String, notFoundMessage As String)
            Dim systemPath = Environment.GetEnvironmentVariable("Path")
            Dim foundDirectory As Boolean
            If Directory.Exists(neededDirectory) Then
                foundDirectory = True
                systemPath = neededDirectory + ";" + systemPath
            End If

            If foundDirectory = False And notFoundMessage.Length > 0 Then
                Debug.WriteLine(neededDirectory + " was not found.  " + notFoundMessage)
                Debug.WriteLine(neededDirectory + " was not found.  " + notFoundMessage)
                Debug.WriteLine(neededDirectory + " was not found.  " + notFoundMessage)
                Debug.WriteLine(neededDirectory + " was not found.  " + notFoundMessage)
                Debug.WriteLine("Review updatePath for directories needed in the path for OpenCVB.")
            End If
            Environment.SetEnvironmentVariable("Path", systemPath)
        End Sub
        Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            settings = settingsIO.Load()
            ' Startup.Splash.loadingLabel.Text = "Starting " + settings.cameraName

            updatePath(homeDir + "bin\", "Release version of CPP_Native.dll")
            updatePath(homeDir + "opencv\Build\bin\Release\", "OpenCV and OpenCV Contrib are needed for C++ classes.")

            updatePath(homeDir + "opencv\Build\bin\Release\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
            updatePath(homeDir + "opencv\Build\bin\Debug\", "OpenCV and OpenCV Contrib are needed for C++ classes.")

            Dim cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH")
            If cudaPath IsNot Nothing And settings.cameraName.StartsWith("StereoLabs") Then
                updatePath(cudaPath, "Cuda - needed for StereoLabs")
                updatePath("C:\Program Files (x86)\ZED SDK\bin", "StereoLabs support")
            End If
            updatePath(homeDir + "OrbbecSDK\lib\win_x64\", "Orbbec camera support.")
            updatePath(homeDir + "OrbbecSDK_CSharp\Build\Debug\", "Orbbec camera support.")
            updatePath(homeDir + "OrbbecSDK_CSharp\Build\Release\", "Orbbec camera support.")  ' 

            updatePath(homeDir + "librealsense\build\Debug\", "Realsense camera support.")
            updatePath(homeDir + "librealsense\build\Release\", "Realsense camera support.")

            updatePath(homeDir + "bin\", "Oak-D camera support.")
            updatePath(homeDir + "OakD\depthai-core\Build\vcpkg_installed\x64-windows\bin\", "Oak-D camera support.")
            'updatePath(homeDir + "OakD\depthai-core\Build\Release", "Oak-D camera support.")
            updatePath(homeDir + "OakD\depthai-core\Build\Debug", "Oak-D camera support.")
            updatePath(homeDir + "OakD\depthai-core\Build\vcpkg_installed\x64-windows\bin", "Oak-D camera support.")

            If settings.cameraPresent(3) Then ' OakD is the 3rd element in cameraPresent but it is not defined explicitly.
                updatePath(homeDir + "OakD\build\Release\", "Luxonis Oak-D camera support.")
            End If

            addPics()

            Me.Location = New Point(settings.MainFormLeft, settings.MainFormTop)
            Me.Size = New Size(settings.MainFormWidth, settings.MainFormHeight)
            Me.Show()

            getLineCounts()

            LoadAvailableAlgorithms()

            setupAlgorithmHistory()

            StartStopTask()

            Const WM_SETICON As Integer = &H80
            Const ICON_SMALL As Integer = 0
            Const ICON_BIG As Integer = 1

            SendMessage(Me.Handle, WM_SETICON, CType(ICON_SMALL, IntPtr), Me.Icon.Handle)
            SendMessage(Me.Handle, WM_SETICON, CType(ICON_BIG, IntPtr), Me.Icon.Handle)
        End Sub

        Private Sub TestAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
            TestAllTimer.Enabled = Not TestAllTimer.Enabled
            Static testAllToolbarBitmap As Bitmap = New Bitmap(homeDir + "MainUI/Data/testall.png")
            Static stopTestAll As Bitmap = New Bitmap(homeDir + "MainUI/Data/stopTestAll.png")
            TestAllButton.Image = If(TestAllTimer.Enabled, stopTestAll, testAllToolbarBitmap)
            If TestAllTimer.Enabled Then
                Debug.WriteLine("")
                Debug.WriteLine("Starting 'TestAll' overnight run.")

                AvailableAlgorithms.Enabled = False  ' the algorithm will be started in the testAllTimer event.
                TestAllTimer.Interval = settings.testAllDuration * 1000
                TestAllTimer.Enabled = True
                task.testAllRunning = True
            Else
                Debug.WriteLine("Stopping 'TestAll' overnight run.")
                AvailableAlgorithms.Enabled = True
                TestAllTimer.Enabled = False
                task.testAllRunning = False
            End If
        End Sub
        Private Sub Pic_Paint(sender As Object, e As PaintEventArgs)
            If task Is Nothing Then Exit Sub

            Dim timeStart As DateTime = Now

            Dim g As Graphics = e.Graphics
            Dim pic = DirectCast(sender, PictureBox)
            g.ScaleTransform(1, 1)

            Dim displayimage = task.dstList(pic.Tag).Resize(New cv.Size(settings.displayRes.Width, settings.displayRes.Height))
            Dim bitmap = cvext.BitmapConverter.ToBitmap(displayimage)

            If pics(pic.Tag).Image IsNot Nothing Then pics(pic.Tag).Image.Dispose()
            g.DrawImage(bitmap, 0, 0)

            labels(pic.Tag).Text = task.labels(pic.Tag)

            Dim ratioX = pic.Width / settings.workRes.Width
            Dim ratioY = pic.Height / settings.workRes.Height

            Dim brush As New SolidBrush(Color.White)
            For Each tt In task.trueData
                If tt.text Is Nothing Then Continue For
                If tt.text.Length > 0 And tt.picTag = pic.Tag Then
                    g.DrawString(tt.text, windowsFont, brush, CSng(tt.pt.X * ratioX), CSng(tt.pt.Y * ratioY))
                End If
            Next
            brush.Dispose()
            bitmap.Dispose()

            Dim timeEnd As DateTime = Now
            Dim elapsedTime = timeEnd.Ticks - timeStart.Ticks
            Dim spanCopy As TimeSpan = New TimeSpan(elapsedTime)
            task.cpu.paintTime += spanCopy.Ticks / TimeSpan.TicksPerMillisecond
        End Sub
        Private Sub startAlgorithm()
            If vbc.task IsNot Nothing Then vbc.task.Dispose()
            vbc.task = New AlgorithmTask

            For i = 0 To pics.Count - 1
                task.dstList(i) = New cv.Mat(settings.workRes, cv.MatType.CV_8UC3, 0)
            Next

            task.color = New cv.Mat(settings.workRes, cv.MatType.CV_8UC3, 0)
            task.pointCloud = New cv.Mat(settings.workRes, cv.MatType.CV_32FC3, 0)
            task.leftView = New cv.Mat(settings.workRes, cv.MatType.CV_8U, 0)
            task.rightView = New cv.Mat(settings.workRes, cv.MatType.CV_8U, 0)
            task.gridRatioX = pics(0).Width / settings.workRes.Width
            task.gridRatioY = pics(0).Height / settings.workRes.Height
            task.homeDir = homeDir
            task.calibData = camera.calibData

            task.main_hwnd = Me.Handle

            task.Initialize(settings)
            task.lowResDepth = New cv.Mat(task.workRes, cv.MatType.CV_32F)
            task.lowResColor = New cv.Mat(task.workRes, cv.MatType.CV_32F)
            task.MainUI_Algorithm = createAlgorithm(settings.algorithm)
            AlgDescription.Text = task.MainUI_Algorithm.desc
            task.resolutionDetails = resolutionDetails

            If task.calibData IsNot Nothing Then task.calibData = camera.calibData

            MainForm_Resize(Nothing, Nothing)
        End Sub
        Private Sub AtoZ_Click(sender As Object, e As EventArgs) Handles AtoZ.Click
            Dim groupsForm As New AtoZ()
            groupsForm.homeDir = New DirectoryInfo(homeDir + "\Data")

            If groupsForm.ShowDialog() = DialogResult.OK AndAlso Not String.IsNullOrEmpty(groupsForm.selectedGroup) Then
                ' Find and select the first algorithm that starts with the selected group
                For Each alg In AvailableAlgorithms.Items
                    Dim algStr = alg.ToString()
                    If Not String.IsNullOrWhiteSpace(algStr) AndAlso algStr.StartsWith(groupsForm.selectedGroup) Then
                        AvailableAlgorithms.SelectedItem = algStr
                        Exit For
                    End If
                Next
            End If
            MainForm_Resize(Nothing, Nothing)
        End Sub
        Private Sub AvailableAlgorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
            settings.algorithm = AvailableAlgorithms.Text
            If TestAllTimer.Enabled = False Then SaveJsonSettings()
            'SaveJsonSettings() ' uncomment this to capture the algorithm that crashes the computer.

            If AvailableAlgorithms.SelectedItem <> " " Then
                settings.algorithm = AvailableAlgorithms.SelectedItem
            Else
                settings.algorithm = AvailableAlgorithms.Items(AvailableAlgorithms.SelectedIndex + 1)
            End If

            startAlgorithm()
            updateAlgorithmHistory()
        End Sub
        Private Sub OptionsButton_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
            If TestAllTimer.Enabled Then TestAllButton_Click(sender, e)

            Debug.WriteLine(vbCrLf + "OptionsTesting GDI: " & GdiMonitor.GetGdiCount())
            Debug.WriteLine("OptionsTesting USER: " & GdiMonitor.GetUserCount())

            StartStopTask()

            Dim optionsForm As New Options()
            If optionsForm.ShowDialog() = DialogResult.OK Then SaveJsonSettings()

            StartStopTask()
            AvailableAlgorithms_SelectedIndexChanged(Nothing, Nothing)

            If task Is Nothing Then startAlgorithm()

            MainForm_Resize(Nothing, Nothing)

            getLineCounts() ' this will update the main form's title with the latest camera name as well as counts.

            optionsForm.Dispose()
        End Sub
        Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
            If task Is Nothing Then Exit Sub

            Debug.Write(Format(totalBytesOfMemoryUsed, "###") + " Mb" + vbCrLf +
                        " " + Format(task.fpsAlgorithm, "0") + " FPS Algorithm" + vbCrLf +
                        " " + Format(task.fpsCamera, "0") + " FPS Camera")

            Static lastTime As DateTime = Now
            Dim timeNow As DateTime = Now
            Static lastWriteTime As DateTime = timeNow

            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
            totalBytesOfMemoryUsed = currentProcess.PrivateMemorySize64 / (1024 * 1024)

            lastWriteTime = timeNow
            If fpsWriteCount = 5 Then
                Debug.WriteLine("")
                fpsWriteCount = 0
            End If
            fpsWriteCount += 1

            If AvailableAlgorithms.Items(AvailableAlgorithms.SelectedIndex + 1) = " " Then
                AvailableAlgorithms.SelectedIndex += 2
            Else
                AvailableAlgorithms.SelectedIndex += 1
            End If
            If AvailableAlgorithms.Items.Count <= AvailableAlgorithms.SelectedIndex + 1 Then AvailableAlgorithms.SelectedIndex = 0

            Debug.WriteLine("Usage GDI: " & GdiMonitor.GetGdiCount() + " USER: " & GdiMonitor.GetUserCount())

            AvailableAlgorithms.SelectedItem = settings.algorithm
        End Sub
        Private Sub StartStopTask()
            isPlaying = Not isPlaying

            If isPlaying Then
                StartCamera()
                AvailableAlgorithms.SelectedItem = settings.algorithm

                MainForm_Resize(Nothing, Nothing)
            Else
                StopCamera()

                If task IsNot Nothing Then ' already stopped...
                    task.readyForCameraInput = False
                    task.Dispose()
                    task = Nothing
                End If
            End If
        End Sub
    End Class
End Namespace
