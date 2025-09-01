Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports VBClasses
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Module OpenCVB_module
    Public cameraLock As New Mutex(True, "cameraLock")
End Module
Namespace OpenCVB
    Partial Public Class Main : Inherits Form
        Public trueTextLock As New Mutex(True, "trueTextLock")
        Public mouseLock As New Mutex(True, "mouseLock") ' global lock for use with mouse clicks. 
        Public algorithmThreadLock As New Mutex(True, "AlgorithmThreadLock")

        Public camPic(4 - 1) As PictureBox
        Public Shared settings As jsonClass.ApplicationStorage
        Public HomeDir As DirectoryInfo
        Public groupButtonSelection As String
        Public fpsAlgorithm As Single
        Public fpsCamera As Single
        Dim fpsWriteCount As Integer
        Dim fpsListA As New List(Of Single)
        Dim fpsListC As New List(Of Single)

        Dim trueData As New List(Of TrueText)
        Dim algolist As algorithmList = New algorithmList
        Dim saveworkRes As cv.Size
        Dim saveCameraName As String
        Dim pauseCameraTask As Boolean

        Dim optionsForm As Options
        Dim groupList As New List(Of String)

        Dim upArrow As Boolean, downArrow As Boolean

        Dim pathList As New List(Of String)

        Dim AlgorithmTestAllCount As Integer
        Dim algorithmCount As Integer
        Dim algorithmTaskHandle As Thread
        Dim algorithmQueueCount As Integer

        Dim saveAlgorithmName As String
        Dim cameraShutdown As Boolean
        Dim shuttingDown As Boolean

        Dim BothFirstAndLastReady As Boolean

        Dim camera As Object

        Dim cameraTaskHandle As Thread
        Dim camLabel(camPic.Count - 1) As Label

        Dim paintNewImages As Boolean
        Dim newCameraImages As Boolean

        Dim algorithmRefresh As Boolean
        Dim CodeLineCount As Integer
        Dim DrawingRectangle As Boolean
        Dim drawRect As New cv.Rect
        Dim drawRectPic As Integer
        Dim frameCount As Integer
        Dim GrabRectangleData As Boolean

        Dim LastX As Integer
        Dim LastY As Integer
        Dim mouseClickFlag As Boolean
        Dim activateTaskForms As Boolean
        Dim ClickPoint As New cv.Point ' last place where mouse was clicked.
        Dim mousePicTag As Integer
        Dim mouseDownPoint As cv.Point
        Dim mouseMovePoint As cv.Point ' last place the mouse was located in any of the OpenCVB images.
        Dim mousePointCamPic As New cv.Point ' mouse location in campics
        Dim activeMouseDown As Boolean

        Dim myBrush = New SolidBrush(Color.White)
        Dim picLabels() = {"", "", "", ""}
        Dim totalBytesOfMemoryUsed As Integer

        Dim uiColor As cv.Mat
        Dim uiLeft As cv.Mat
        Dim uiRight As cv.Mat
        Dim uiPointCloud As cv.Mat

        Dim pauseAlgorithmThread As Boolean
        Dim logAlgorithms As StreamWriter

        Const MAX_RECENT = 50
        Dim algHistory As New List(Of String)
        Dim arrowList As New List(Of String)
        Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
        Dim arrowIndex As Integer

        Dim pixelViewerRect As cv.Rect
        Dim pixelViewerOn As Boolean
        Dim pixelViewTag As Integer

        Dim PausePlay As Bitmap
        Dim runPlay As Bitmap
        Dim stopTestAll As Bitmap
        Dim complexityTest As Bitmap
        Dim complexityResults As New List(Of String)
        Dim complexityStartTime As DateTime

        Dim testAllToolbarBitmap As Bitmap

        Dim testAllRunning As Boolean

        Public colorTransitionCount As Integer
        Public colorScheme As String
        Dim pythonPresent As Boolean

        Dim testAllResolutionCount As Integer
        Dim testAllStartingRes As Integer
        Dim testAllEndingRes As Integer

        Dim magnifyIndex As Integer
        Dim depthAndCorrelationText As String
        Dim results As New Comm.resultData
        Public jsonfs As New jsonClass.jsonIO
        Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
            Dim args() = Environment.GetCommandLineArgs()

            Dim exePath As String = AppDomain.CurrentDomain.BaseDirectory
            Dim solutionDir As String = Path.GetFullPath(Path.Combine(exePath, "..\..\..\..\..\..\"))
            HomeDir = New DirectoryInfo(solutionDir)

            Dim executingAssemblyPath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            Dim exeDir = New DirectoryInfo(Path.GetDirectoryName(executingAssemblyPath))
            Directory.SetCurrentDirectory(HomeDir.FullName)

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture

            jsonfs.jsonFileName = HomeDir.FullName + "settings.json"
            settings = jsonfs.read()

            optionsForm = New Options
            optionsForm.defineCameraResolutions(settings.cameraIndex)

            setupPath()
            camSwitch()
            Application.DoEvents()

            PausePlay = New Bitmap(HomeDir.FullName + "Main/Data/PauseButton.png")
            stopTestAll = New Bitmap(HomeDir.FullName + "Main/Data/stopTestAll.png")
            testAllToolbarBitmap = New Bitmap(HomeDir.FullName + "Main/Data/testall.png")
            runPlay = New Bitmap(HomeDir.FullName + "Main/Data/PauseButtonRun.png")

            setupAlgorithmHistory()

            Dim libraries = {"Cam_K4A.dll", "CPP_Native.dll", "Cam_MyntD.dll", "Cam_Zed2.dll", "Cam_ORB335L.dll"}
            For i = 0 To libraries.Count - 1
                Dim dllName = libraries(i)
                Dim dllFile = New FileInfo(HomeDir.FullName + "\bin\Debug\" + dllName)
                If dllFile.Exists Then
                    ' if the debug dll exists, then remove the Release version because Release is ahead of Debug in the path for this app.
                    Dim releaseDLL = New FileInfo(HomeDir.FullName + "\bin\Release\" + dllName)
                    If releaseDLL.Exists Then
                        If DateTime.Compare(dllFile.LastWriteTime, releaseDLL.LastWriteTime) > 0 Then releaseDLL.Delete() Else dllFile.Delete()
                    End If
                End If
            Next

            Me.Show()
            frameCount = 0
            setupCamPics()

            loadAlgorithmComboBoxes()

            GroupComboBox.Text = settings.groupComboText

            If GroupComboBox.SelectedItem() Is Nothing Then
                Dim group = GroupComboBox.Text
                If InStr(group, ") ") Then
                    Dim offset = InStr(group, ") ")
                    group = group.Substring(offset + 2)
                End If
                For i = 0 To GroupComboBox.Items.Count - 1
                    If GroupComboBox.Items(i).contains(group) Then
                        GroupComboBox.SelectedItem() = GroupComboBox.Items(i)
                        settings.groupComboText = GroupComboBox.Text
                        Exit For
                    End If
                Next
            End If

            If AvailableAlgorithms.Items.Count = 0 Then
                MessageBox.Show("There were no algorithms listed for the " + GroupComboBox.Text + vbCrLf +
                           "This usually indicates something has changed with " + vbCrLf + "UIGenerator")
            Else
                If settings.MainUI_AlgName Is Nothing Then
                    AvailableAlgorithms.SelectedIndex = 0
                    settings.MainUI_AlgName = AvailableAlgorithms.Text
                End If
                If AvailableAlgorithms.Items.Contains(settings.MainUI_AlgName) Then
                    AvailableAlgorithms.Text = settings.MainUI_AlgName
                Else
                    AvailableAlgorithms.SelectedIndex = 0
                End If
                jsonfs.write()
            End If

            AvailableAlgorithms.ComboBox.Select()

            fpsTimer.Enabled = True
            XYLoc.Text = "(x:0, y:0) - last click point at: (x:0, y:0)"
            XYLoc.Visible = True

            If settings.cameraFound Then initCamera()

            Debug.WriteLine("")
            Debug.WriteLine("Main_Load complete.")
        End Sub

        Private Sub OpenCVB_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
            If e.KeyValue = Keys.Up Then upArrow = True
            If e.KeyValue = Keys.Down Then downArrow = True
        End Sub
        Public Function validateRect(r As cv.Rect, width As Integer, height As Integer) As cv.Rect
            If r.Width < 0 Then r.Width = 1
            If r.Height < 0 Then r.Height = 1
            If r.X < 0 Then r.X = 0
            If r.Y < 0 Then r.Y = 0
            If r.X > width Then r.X = width - 1
            If r.Y > height Then r.Y = height - 1
            If r.X + r.Width > width Then r.Width = width - r.X - 1
            If r.Y + r.Height > height Then r.Height = height - r.Y - 1
            Return r
        End Function
        Public Function validatePoint(pt As cv.Point2f) As cv.Point
            If pt.X < 0 Then pt.X = 0
            If pt.X > task.workRes.Width Then pt.X = task.workRes.Width - 1
            If pt.Y < 0 Then pt.Y = 0
            If pt.Y > task.workRes.Height Then pt.Y = task.workRes.Height - 1
            Return pt
        End Function
        Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
            jsonfs.write()
            cameraShutdown = True
            Thread.Sleep(200)
            End
        End Sub
        Private Sub OptionsButton_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
            If TestAllTimer.Enabled Then TestAllButton_Click(sender, e)
            Dim saveCameraIndex = settings.cameraIndex

            optionsForm.MainOptions_Load(sender, e)
            optionsForm.cameraRadioButton(settings.cameraIndex).Checked = True
            Dim resStr = CStr(settings.workRes.Width) + "x" + CStr(settings.workRes.Height)
            For i = 0 To Comm.resolutionList.Count - 1
                If Comm.resolutionList(i).StartsWith(resStr) Then
                    optionsForm.workResRadio(i).Checked = True
                End If
            Next

            Dim OKcancel = optionsForm.ShowDialog()

            If OKcancel = DialogResult.OK Then
                pauseCameraTask = True
                task.optionsChanged = True
                If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e)
                settings.workRes = optionsForm.cameraworkRes
                settings.displayRes = optionsForm.cameraDisplayRes
                settings.cameraName = optionsForm.cameraName
                settings.cameraIndex = optionsForm.cameraIndex
                settings.testAllDuration = optionsForm.testDuration

                frameCount = 0
                setupCamPics()

                camSwitch()

                jsonfs.write()
                settings = jsonfs.read() ' this will apply all the changes...
                Application.DoEvents()

                StartAlgorithm()
                pauseCameraTask = False
            Else
                settings.cameraIndex = saveCameraIndex
            End If
        End Sub
        Private Sub CamPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            Try
                If DrawingRectangle Then
                    DrawingRectangle = False
                    GrabRectangleData = True
                End If
                activeMouseDown = False
            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseUp: " + ex.Message)
            End Try
        End Sub

        Private Sub CamPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
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
                    mouseDownPoint.X = e.X
                    mouseDownPoint.Y = e.Y
                End If
            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseDown: " + ex.Message)
            End Try
        End Sub
        Private Sub CamPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
            Try
                Dim pic = DirectCast(sender, PictureBox)
                If activeMouseDown Then Exit Sub
                If DrawingRectangle Then
                    mouseMovePoint.X = e.X
                    mouseMovePoint.Y = e.Y
                    If mouseMovePoint.X < 0 Then mouseMovePoint.X = 0
                    If mouseMovePoint.Y < 0 Then mouseMovePoint.Y = 0
                    drawRectPic = pic.Tag
                    If e.X < camPic(0).Width Then
                        drawRect.X = Math.Min(mouseDownPoint.X, mouseMovePoint.X)
                    Else
                        drawRect.X = Math.Min(mouseDownPoint.X - camPic(0).Width, mouseMovePoint.X - camPic(0).Width)
                        drawRectPic = 3 ' When wider than campic(0), it can only be dst3 which has no pic.tag (because campic(2) is double-wide for timing reasons.
                    End If
                    drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                    drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                    drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                    If drawRect.X + drawRect.Width > camPic(0).Width Then drawRect.Width = camPic(0).Width - drawRect.X
                    If drawRect.Y + drawRect.Height > camPic(0).
                        Height Then drawRect.Height = camPic(0).Height - drawRect.Y
                    BothFirstAndLastReady = True
                End If

                mousePicTag = pic.Tag
                mousePointCamPic.X = e.X
                mousePointCamPic.Y = e.Y
                mousePointCamPic *= settings.workRes.Width / camPic(0).Width

                XYLoc.Text = mousePointCamPic.ToString + ", last click point at: " + ClickPoint.ToString

            Catch ex As Exception
                Debug.WriteLine("Error in camPic_MouseMove: " + ex.Message)
            End Try
        End Sub
        Private Sub Campic_Click(sender As Object, e As EventArgs)
            SyncLock mouseLock
                mouseClickFlag = True
            End SyncLock
            activateTaskForms = True
        End Sub
        Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
            DrawingRectangle = False
        End Sub
        Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
            Dim g As Graphics = e.Graphics
            Dim pic = DirectCast(sender, PictureBox)
            Dim ratio = camPic(2).Width / settings.workRes.Width
            g.ScaleTransform(1, 1)
            g.DrawImage(pic.Image, 0, 0)

            Static myWhitePen As New Pen(Color.White)
            Static myBlackPen As New Pen(Color.Black)

            If pixelViewerOn And mousePicTag = pic.Tag Then
                Dim r = pixelViewerRect
                Dim rect = New cv.Rect(CInt(r.X * ratio), CInt(r.Y * ratio),
                                   CInt(r.Width * ratio), CInt(r.Height * ratio))
                g.DrawRectangle(myWhitePen, rect.X, rect.Y, rect.Width, rect.Height)
            End If

            If drawRect.Width > 0 And drawRect.Height > 0 Then
                g.DrawRectangle(myWhitePen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
                If pic.Tag = 2 Then
                    g.DrawRectangle(myWhitePen, drawRect.X + camPic(0).Width, drawRect.Y,
                                drawRect.Width, drawRect.Height)
                End If
            End If

            If paintNewImages Then
                paintNewImages = False
                If uiColor IsNot Nothing Then
                    If CameraSwitching.Visible Then
                        CameraSwitching.Visible = False
                        CamSwitchProgress.Visible = False
                        CamSwitchTimer.Enabled = False
                    End If
                    If uiColor.Width > 0 Then
                        Dim camSize = New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height)
                        If results.dstList IsNot Nothing Then
                            If results.dstList(0).Width > 0 Then
                                SyncLock task.resultLock
                                    For i = 0 To results.dstList.Count - 1
                                        Dim tmp = results.dstList(i)
                                        tmp.Circle(mousePointCamPic, task.DotSize + 1, cv.Scalar.White, -1)
                                        tmp = tmp.Resize(camSize)
                                        cvext.BitmapConverter.ToBitmap(tmp, camPic(i).Image)
                                    Next
                                End SyncLock

                                trueData.Add(New TrueText(task.depthAndCorrelationText,
                                             New cv.Point(mousePointCamPic.X, mousePointCamPic.Y - 24), 1))
                            End If
                        End If
                    End If
                End If
            End If

            ' draw any TrueType font data on the image 
            SyncLock trueTextLock
                For i = 0 To trueData.Count - 1
                    Dim tt = trueData(i)
                    If tt.text Is Nothing Then Continue For
                    If tt.text.Length > 0 And tt.picTag = pic.Tag Then
                        g.DrawString(tt.text, settings.fontInfo, New SolidBrush(Color.White),
                                     CSng(tt.pt.X * ratio), CSng(tt.pt.Y * ratio))
                    End If
                Next
            End SyncLock

            Dim workRes = settings.workRes
            Dim cres = settings.captureRes
            Dim dres = settings.displayRes
            Dim resolutionDetails = "Input " + CStr(cres.Width) + "x" + CStr(cres.Height) + ", workRes " +
                                           CStr(workRes.Width) + "x" + CStr(workRes.Height)
            If picLabels(0) <> "" Then
                If camLabel(0).Text <> picLabels(0) + " - RGB " + resolutionDetails Then
                    camLabel(0).Text = picLabels(0)
                    camLabel(0).Text += " - RGB " + resolutionDetails
                End If
            Else
                camLabel(0).Text = "RGB - " + resolutionDetails
            End If

            If picLabels(1) <> "" Then camLabel(1).Text = picLabels(1)
            camLabel(1).Text = picLabels(1)
            camLabel(2).Text = picLabels(2)
            camLabel(3).Text = picLabels(3)

            ' why run all SharpGL algorithms here?  Because too much data has to move from task to main.
            If AvailableAlgorithms.Text = "GL_MainForm" Then
                Static saveFrame = frameCount
                If saveFrame <> frameCount Then
                    saveFrame = frameCount
                    updateSharpGL()
                End If
            End If
        End Sub
        Private Sub setupCamPics()
            ' when you change the primary monitor, old coordinates can go way off the screen.
            Dim goodPoint = Screen.GetWorkingArea(New Point(Me.Left, Me.Top))
            If goodPoint.X > Me.Left Then Me.Left = goodPoint.X
            If goodPoint.Y > Me.Top Then Me.Top = goodPoint.Y

            If camLabel(0) Is Nothing Then
                For i = 0 To camLabel.Length - 1
                    camLabel(i) = New Label
                    camLabel(i).Height -= 7
                    camLabel(i).AutoSize = True
                    Me.Controls.Add(camLabel(i))
                    camLabel(i).Visible = True
                Next

                For i = 0 To camPic.Length - 1
                    camPic(i) = New PictureBox()
                    AddHandler camPic(i).DoubleClick, AddressOf campic_DoubleClick
                    AddHandler camPic(i).Click, AddressOf Campic_Click
                    AddHandler camPic(i).Paint, AddressOf campic_Paint
                    AddHandler camPic(i).MouseDown, AddressOf CamPic_MouseDown
                    AddHandler camPic(i).MouseUp, AddressOf CamPic_MouseUp
                    AddHandler camPic(i).MouseMove, AddressOf CamPic_MouseMove
                    camPic(i).Tag = i
                    camPic(i).Size = New Size(settings.workRes.Width, settings.workRes.Height)
                    Me.Controls.Add(camPic(i))
                Next
            End If
            LineUpCamPics()
        End Sub
        Private Sub OpenCVB_Resize(sender As Object, e As EventArgs) Handles Me.Resize
            If camPic Is Nothing Then Exit Sub ' when first opening, campic may not be built yet
            If camPic(2) Is Nothing Then Exit Sub ' individual pictureboxes need to be ready as well.
            LineUpCamPics()
            If settings.snap320 Or settings.snap640 Then
                XYLoc.Text = "To resize OpenCVB's main window, first set the global option for 'Custom' size."
            End If
        End Sub
        Private Sub LineUpCamPics()
            Dim imgHeight = settings.displayRes.Height
            Dim imgWidth = settings.displayRes.Width
            If settings.snap640 Then imgWidth = 640
            If settings.snap320 Then imgWidth = 320
            Dim padX = 12
            Dim padY = 35
            If settings.snapCustom Then ' custom size - neither snap320 or snap640
                Dim ratio = settings.workRes.Height / settings.workRes.Width
                imgWidth = Me.Width / 2 - padX * 2
                imgHeight = CInt(imgWidth * ratio)
            End If
            camPic(0).Size = New Size(imgWidth, imgHeight)
            camPic(1).Size = New Size(imgWidth, imgHeight)
            camPic(2).Size = New Size(imgWidth, imgHeight)
            camPic(3).Size = New Size(imgWidth, imgHeight)

            camPic(0).Image = New Bitmap(imgWidth, imgHeight, Imaging.PixelFormat.Format24bppRgb)
            camPic(1).Image = New Bitmap(imgWidth, imgHeight, Imaging.PixelFormat.Format24bppRgb)
            camPic(2).Image = New Bitmap(imgWidth, imgHeight, Imaging.PixelFormat.Format24bppRgb)
            camPic(3).Image = New Bitmap(imgWidth, imgHeight, Imaging.PixelFormat.Format24bppRgb)
            camPic(0).Location = New Point(padX, padY + camLabel(0).Height)
            camPic(1).Location = New Point(camPic(0).Left + camPic(0).Width, padY + camLabel(0).Height)
            camPic(2).Location = New Point(padX, camPic(0).Top + camPic(0).Height + camLabel(0).Height)
            camPic(3).Location = New Point(camPic(1).Left, camPic(2).Top)

            If camLabel(0).Location <> New Point(padX, camPic(0).Top) Then
                camLabel(0).Location = New Point(padX, camPic(0).Top - camLabel(0).Height)
                camLabel(1).Location = New Point(padX + camPic(0).Width, camLabel(0).Location.Y)
                camLabel(2).Location = New Point(padX, camPic(2).Top - camLabel(2).Height)
                camLabel(3).Location = New Point(padX + camPic(0).Width, padY + camPic(0).Height + camLabel(0).Height)
            End If

            If depthAndCorrelationText <> "" Then camLabel(1).Text = depthAndCorrelationText

            XYLoc.Location = New Point(camPic(2).Left, camPic(2).Top + camPic(2).Height)
            AlgDescription.Visible = settings.snap640
            GroupComboBox.Visible = settings.snap640
            If settings.snap320 Then Me.Width = 720 ' expose the list of available algorithms.
        End Sub
        Private Sub Magnify_Click(sender As Object, e As EventArgs) Handles Magnify.Click
            MagnifyTimer.Enabled = True
            magnifyIndex += 1
        End Sub
        Private Sub MagnifyTimer_Tick(sender As Object, e As EventArgs) Handles MagnifyTimer.Tick
            Dim ratio = saveworkRes.Width / camPic(0).Width
            Dim r = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
            r = validateRect(r, task.results.dstList(drawRectPic).Width, task.results.dstList(drawRectPic).Height)
            If r.Width = 0 Or r.Height = 0 Then Exit Sub
            Dim img = task.results.dstList(drawRectPic)(r).Resize(New cv.Size(drawRect.Width * 5, drawRect.Height * 5))
            cv.Cv2.ImShow("DrawRect Region " + CStr(magnifyIndex), img)
        End Sub
        Private Sub camSwitch()
            CameraSwitching.Visible = True
            CameraSwitching.Text = settings.cameraName + " initializing"
            CamSwitchProgress.Visible = True
            CamSwitchProgress.Left = CameraSwitching.Left
            CamSwitchProgress.Top = CameraSwitching.Top + CameraSwitching.Height
            CamSwitchProgress.Height = CameraSwitching.Height / 2
            CameraSwitching.BringToFront()
            CamSwitchProgress.BringToFront()
            uiColor = Nothing
            CamSwitchTimer.Enabled = True
        End Sub
        Private Sub CamSwitchTimer_Tick(sender As Object, e As EventArgs) Handles CamSwitchTimer.Tick
            If settings Is Nothing Then
                CamSwitchProgress.Visible = False
                CameraSwitching.Visible = False
                Exit Sub
            End If
            If settings.cameraName <> "" Then
                If CamSwitchProgress.Visible Then
                    Static frames As Integer
                    Dim slideCount As Integer = 10
                    CamSwitchProgress.Width = CameraSwitching.Width * frames / slideCount
                    If frames >= slideCount Then frames = 0
                    frames += 1
                End If
            Else
                CamSwitchProgress.Visible = False
                CameraSwitching.Visible = False
            End If
        End Sub
        Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
            Static lastTime As DateTime = Now
            Static lastAlgorithmFrame As Integer
            Static lastCameraFrame As Integer
            If camera Is Nothing Then Exit Sub
            If lastAlgorithmFrame > frameCount Then lastAlgorithmFrame = 0
            If lastCameraFrame > camera.cameraFrameCount Then lastCameraFrame = 0

            If pauseAlgorithmThread = False Then
                Dim timeNow As DateTime = Now
                Dim elapsedTime = timeNow.Ticks - lastTime.Ticks
                Dim spanCopy As TimeSpan = New TimeSpan(elapsedTime)
                Dim taskTimerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                lastTime = timeNow

                Dim countFrames = frameCount - lastAlgorithmFrame
                Dim camFrames = camera.cameraFrameCount - lastCameraFrame
                lastAlgorithmFrame = frameCount
                lastCameraFrame = camera.cameraFrameCount

                If taskTimerInterval > 0 Then
                    fpsListA.Add(CSng(countFrames / (taskTimerInterval / 1000)))
                    fpsListC.Add(CSng(camFrames / (taskTimerInterval / 1000)))
                Else
                    fpsListA.Add(0)
                    fpsListC.Add(0)
                End If

                If cameraTaskHandle Is Nothing Then Exit Sub
                CameraSwitching.Text = AvailableAlgorithms.Text + " awaiting first buffer"
                Dim cameraName = settings.cameraName
                cameraName = cameraName.Replace(" 2/2i", "")
                cameraName = cameraName.Replace(" camera", "")
                cameraName = cameraName.Replace(" Camera", "")
                cameraName = cameraName.Replace("Intel(R) RealSense(TM) Depth ", "Intel D")

                fpsAlgorithm = fpsListA.Average
                fpsCamera = CInt(fpsListC.Average)
                If fpsAlgorithm >= 100 Then fpsAlgorithm = 99
                If fpsCamera >= 100 Then fpsCamera = 99
                Me.Text = "OpenCVB - " + Format(CodeLineCount, "###,##0") + " lines / " +
                          CStr(algorithmCount) + " algorithms = " +
                          CStr(CInt(CodeLineCount / algorithmCount)) + " lines each (avg) - " +
                          cameraName + " - Camera FPS/task FPS: " + Format(fpsCamera, "0") + "/" +
                          Format(fpsAlgorithm, "0")
                If fpsListA.Count > 5 Then
                    fpsListA.RemoveAt(0)
                    fpsListC.RemoveAt(0)
                End If

                If fpsAlgorithm = 0 Then
                    fpsAlgorithm = 1
                Else
                    Static lastWriteTime = timeNow
                    elapsedTime = timeNow.Ticks - lastWriteTime.Ticks
                    spanCopy = New TimeSpan(elapsedTime)
                    taskTimerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                    If taskTimerInterval > If(testAllRunning, 1000, 5000) Then
                        Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
                        totalBytesOfMemoryUsed = currentProcess.PrivateMemorySize64 / (1024 * 1024)

                        lastWriteTime = timeNow
                        If fpsWriteCount = 5 Then
                            Debug.WriteLine("")
                            fpsWriteCount = 0
                        End If
                        fpsWriteCount += 1
                        Debug.Write(" " + Format(totalBytesOfMemoryUsed, "###") + "/" + Format(fpsAlgorithm, fmt0) + "/" +
                                          Format(fpsCamera, fmt0))
                    End If
                End If
            End If
        End Sub
        Private Sub setupTestAll()
            testAllResolutionCount = 0
            testAllStartingRes = -1
            For i = 0 To settings.resolutionsSupported.Count - 1
                If settings.resolutionsSupported(i) Then testAllResolutionCount += 1
                If testAllStartingRes < 0 And settings.resolutionsSupported(i) Then testAllStartingRes = i
                If settings.resolutionsSupported(i) Then testAllEndingRes = i
            Next
        End Sub
        Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
            Static saveTestAllState As Boolean
            Static algorithmRunning = True
            If PausePlayButton.Text = "Run" Then
                PausePlayButton.Text = "Pause"
                pauseAlgorithmThread = False
                If saveTestAllState Then TestAllButton_Click(sender, e)
                PausePlayButton.Image = PausePlay
            Else
                fpsWriteCount = 0
                PausePlayButton.Text = "Run"
                pauseAlgorithmThread = True
                saveTestAllState = TestAllTimer.Enabled
                If TestAllTimer.Enabled Then TestAllButton_Click(sender, e)
                PausePlayButton.Image = runPlay
            End If
        End Sub
        Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
            ' don't start another algorithm until the current one has finished 
            If algorithmQueueCount <> 0 Then
                ' Give the algorithm a reasonable time to finish, then crash.
                Dim crash As Boolean = True
                For i = 0 To 10
                    Thread.Sleep(2000)
                    If algorithmQueueCount = 0 Then
                        crash = False
                        Exit For
                    End If
                Next
                If crash Then
                    Throw New InvalidOperationException("Can't start the next algorithm because previous algorithm has not completed.")
                End If
            End If

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

            TestAllTimer.Interval = settings.testAllDuration * 1000
            Static startingAlgorithm = AvailableAlgorithms.Text
            If AvailableAlgorithms.Text = startingAlgorithm And AlgorithmTestAllCount > 1 Then
                If settings.workResIndex > testAllEndingRes Then
                    While 1
                        settings.cameraIndex += 1
                        If settings.cameraIndex >= Comm.cameraNames.Count - 1 Then settings.cameraIndex = 0
                        settings.cameraName = Comm.cameraNames(settings.cameraIndex)
                        If settings.cameraPresent(settings.cameraIndex) Then
                            Options.defineCameraResolutions(settings.cameraIndex)
                            setupTestAll()
                            settings.workResIndex = testAllStartingRes + 3 ' skip highest res and try 1280x720
                            Exit While
                        End If
                    End While
                    ' extra time for the camera to restart...
                    TestAllTimer.Interval = settings.testAllDuration * 1000 * 3
                End If

                jsonfs.write()
                settings = jsonfs.read()
                LineUpCamPics()

                If testAllRunning = False Then
                    Dim resStr = CStr(settings.workRes.Width) + "x" + CStr(settings.workRes.Height)
                    For i = 0 To Comm.resolutionList.Count - 1
                        If Comm.resolutionList(i).StartsWith(resStr) Then
                            settings.workResIndex = i
                            Exit For
                        End If
                    Next
                End If

                Options.setworkRes()

                ' when switching resolution, best to reset these as the move from higher to lower res
                ' could mean the point is no longer valid.
                ClickPoint = New cv.Point
                mousePointCamPic = New cv.Point
            End If

            Static saveLastAlgorithm = AvailableAlgorithms.Text
            If saveLastAlgorithm <> AvailableAlgorithms.Text Then
                settings.workResIndex += 1
                saveLastAlgorithm = AvailableAlgorithms.Text
            End If
            StartAlgorithm()
        End Sub
        Private Sub TestAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
            TestAllButton.Image = If(TestAllButton.Text = "Test All", stopTestAll, testAllToolbarBitmap)
            If TestAllButton.Text = "Test All" Then
                Debug.WriteLine("")
                Debug.WriteLine("Starting 'TestAll' overnight run.")
                AlgorithmTestAllCount = 1

                setupTestAll()

                TestAllButton.Text = "Stop Test"
                AvailableAlgorithms.Enabled = False  ' the algorithm will be started in the testAllTimer event.
                jsonfs.write()
                settings = jsonfs.read()

                TestAllTimer.Interval = 1
                TestAllTimer.Enabled = True
            Else
                AvailableAlgorithms.Enabled = True
                TestAllTimer.Enabled = False
                TestAllButton.Text = "Test All"
            End If
            testAllRunning = TestAllTimer.Enabled
        End Sub
        Private Sub updatePath(neededDirectory As String, notFoundMessage As String)
            Dim systemPath = Environment.GetEnvironmentVariable("Path")
            Dim foundDirectory As Boolean
            If Directory.Exists(neededDirectory) Then
                foundDirectory = True
                systemPath = neededDirectory + ";" + systemPath
                pathList.Add(neededDirectory) ' used only for debugging the path.
            End If

            If foundDirectory = False And notFoundMessage.Length > 0 Then
                MessageBox.Show(neededDirectory + " was not found.  " + notFoundMessage)
            End If
            Environment.SetEnvironmentVariable("Path", systemPath)
        End Sub
        Private Sub setupPath()
            ' Camera DLL's and OpenGL apps are built in Release mode even when configured for Debug (performance is much better).  
            ' OpenGL apps cannot be debugged from OpenCVB and the camera interfaces are not likely to need debugging.
            ' To debug a camera interface: change the Build Configuration and enable "Native Code Debugging" in the OpenCVB project.
            updatePath(HomeDir.FullName + "bin\", "Debug/Release version of CPP_Native.dll")
            updatePath(HomeDir.FullName + "bin\Release\", "Release Version of camera DLL's.")
            Dim testDebug As New DirectoryInfo(HomeDir.FullName + "bin\Debug\")
            If testDebug.Exists Then
                updatePath(HomeDir.FullName + "bin\Debug\", "Debug Version of any camera DLL's.")
            End If

            ' CPP_Native.dll only exists in the homedir.fullname + "bin" directory.
            ' Turn optimizations on and off by modifying C/C++ Optimizations and Basic Runtime Checking.
            ' VB knows CPP_Native.dll and doesn't switch the debug version (there's a way to do it tho.)

            updatePath(HomeDir.FullName + "opencv\Build\bin\Release\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
            updatePath(HomeDir.FullName + "opencv\Build\bin\Debug\", "OpenCV and OpenCV Contrib are needed for C++ classes.")

            Dim cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH")
            Dim zedIndex = Comm.cameraNames.IndexOf("StereoLabs ZED 2/2i")
            If cudaPath IsNot Nothing And settings.cameraPresent(zedIndex) And settings.cameraSupported(zedIndex) = True Then
                updatePath(cudaPath, "Cuda - needed for StereoLabs")
                updatePath("C:\Program Files (x86)\ZED SDK\bin", "StereoLabs support")
            End If
            updatePath(HomeDir.FullName + "OrbbecSDK\lib\win_x64\", "Orbbec camera support.")
            updatePath(HomeDir.FullName + "OrbbecSDK_CSharp\Build\Debug\", "Orbbec camera support.")
            updatePath(HomeDir.FullName + "OrbbecSDK_CSharp\Build\Release\", "Orbbec camera support.")
            updatePath(HomeDir.FullName + "OrbbecSDK\lib\win_x64\", "OrbbecSDK.dll")

            updatePath(HomeDir.FullName + "librealsense\build\Debug\", "Realsense camera support.")
            updatePath(HomeDir.FullName + "librealsense\build\Release\", "Realsense camera support.")

            If settings.cameraPresent(3) Then ' OakD is the 3rd element in cameraPresent but it is not defined explicitly.
                updatePath(HomeDir.FullName + "OakD\build\Release\", "Luxonis Oak-D camera support.")
            End If
        End Sub
        Private Sub AvailableAlgorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
            If Trim(AvailableAlgorithms.Text) = "" Then
                Dim incr = 1
                If upArrow Then incr = -1
                upArrow = False
                downArrow = False
                AvailableAlgorithms.Text = AvailableAlgorithms.Items(AvailableAlgorithms.SelectedIndex + incr)
                Exit Sub
            End If
            If AvailableAlgorithms.Enabled Then
                If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e) ' if paused, then restart.
                jsonfs.write()
                StartAlgorithm()
                updateAlgorithmHistory()
            End If
        End Sub
        Private Sub groupName_SelectedIndexChanged(sender As Object, e As EventArgs) Handles GroupComboBox.SelectedIndexChanged
            If GroupComboBox.Text = "" Then
                Dim incr = 1
                If upArrow Then incr = -1
                upArrow = False
                downArrow = False
                GroupComboBox.Text = GroupComboBox.Items(GroupComboBox.SelectedIndex + incr)
                Exit Sub
            End If

            AvailableAlgorithms.Enabled = False
            Dim keyIndex = GroupComboBox.Items.IndexOf(GroupComboBox.Text)
            Dim groupings = groupList(keyIndex)
            Dim split = Regex.Split(groupings, ",")
            AvailableAlgorithms.Items.Clear()
            Dim lastSplit As String = ""
            For i = 1 To split.Length - 1
                If split(i).StartsWith("Options_") Then Continue For
                If split(i).StartsWith("CPP_Basics") Then Continue For
                Dim namesplit = split(i).Split("_")
                If lastSplit <> namesplit(0) And lastSplit <> "" Then
                    AvailableAlgorithms.Items.Add(" ")
                End If
                AvailableAlgorithms.Items.Add(split(i))
                lastSplit = namesplit(0)
            Next
            AvailableAlgorithms.Enabled = True

            If GroupComboBox.Text.Contains("All") = False Then algHistory.Clear()

            ' if the fpstimer is enabled, then OpenCVB is running - not initializing.
            If fpsTimer.Enabled Then
                If AvailableAlgorithms.Items.Contains(settings.MainUI_AlgName) Then
                    AvailableAlgorithms.Text = settings.MainUI_AlgName
                Else
                    AvailableAlgorithms.SelectedIndex = 0
                End If
            End If
        End Sub
        Private Sub loadAlgorithmComboBoxes()
            Dim countFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmCounts.txt")
            If countFileInfo.Exists = False Then
                MessageBox.Show("The AlgorithmCounts.txt file is missing.  Run 'UI_Generator' or rebuild all to rebuild the user interface.")
            End If
            Dim sr = New StreamReader(countFileInfo.FullName)

            Dim infoLine = sr.ReadLine
            Dim Split = Regex.Split(infoLine, "\W+")
            CodeLineCount = Split(1)

            infoLine = sr.ReadLine
            Split = Regex.Split(infoLine, "\W+")
            algorithmCount = Split(1)
            sr.Close()

            Dim groupFileInfo = New FileInfo(HomeDir.FullName + "Data/GroupComboBox.txt")
            If groupFileInfo.Exists = False Then
                MessageBox.Show("The groupFileInfo.txt file is missing.  Run 'UI_Generator' or Clean/Rebuild to get the user interface.")
            End If
            sr = New StreamReader(groupFileInfo.FullName)
            GroupComboBox.Items.Clear()
            While sr.EndOfStream = False
                infoLine = sr.ReadLine
                Split = infoLine.Split(",")
                groupList.Add(infoLine)
                GroupComboBox.Items.Add(Split(0))
            End While
            sr.Close()
        End Sub

        Private Sub AtoZButton_Click(sender As Object, e As EventArgs) Handles AtoZButton.Click
            Groups_AtoZ.homeDir = HomeDir
            Groups_AtoZ.ShowDialog()
            If groupButtonSelection = "" Then Exit Sub
            If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e) ' if paused, then restart.
            For Each alg In AvailableAlgorithms.Items
                If alg.startswith(groupButtonSelection) Then
                    AvailableAlgorithms.Text = alg
                    Exit For
                End If
            Next

            jsonfs.write()
            StartAlgorithm()
            updateAlgorithmHistory()
            groupButtonSelection = ""
        End Sub
        Public Sub jumpToAlgorithm(algName As String)
            If AvailableAlgorithms.Items.Contains(algName) = False Then
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedItem = algName
            End If
            settings.MainUI_AlgName = AvailableAlgorithms.Text
            jsonfs.write()
        End Sub
        Private Sub algHistory_Clicked(sender As Object, e As EventArgs)
            arrowIndex = 0
            Dim item = TryCast(sender, ToolStripMenuItem)
            If AvailableAlgorithms.Items.Contains(item.Text) = False Then
                MessageBox.Show("That algorithm was not found" + vbCrLf + vbCrLf + "The name may have changed or " + vbCrLf +
                       "The currently selected group does not contain " + item.Text + vbCrLf + "Change the group to <All> to guarantee access.")
            Else
                jumpToAlgorithm(item.Text)
            End If
        End Sub
        Private Sub updateAlgorithmHistory()
            If TestAllTimer.Enabled Then Exit Sub
            Dim copyList As List(Of String)
            If algHistory.Contains(AvailableAlgorithms.Text) Then
                ' make it the most recent
                copyList = New List(Of String)(algHistory)
                algHistory.Clear()
                algHistory.Add(AvailableAlgorithms.Text)
                For i = 0 To copyList.Count - 1
                    If algHistory.Contains(copyList(i)) = False Then algHistory.Add(copyList(i))
                Next
            Else
                If algHistory.Count > 0 Then algHistory.RemoveAt(algHistory.Count - 1)
                copyList = New List(Of String)(algHistory)
                algHistory.Clear()
                algHistory.Add(AvailableAlgorithms.Text)
                For i = 0 To copyList.Count - 1
                    If algHistory.Contains(copyList(i)) = False Then algHistory.Add(copyList(i))
                Next
            End If
            RecentList.DropDownItems.Clear()
            For i = 0 To algHistory.Count - 1
                RecentList.DropDownItems.Add(algHistory(i))
                AddHandler RecentList.DropDownItems(i).Click, AddressOf algHistory_Clicked
                SaveSetting("OpenCVB", "algHistory" + CStr(i), "algHistory" + CStr(i), algHistory(i))
            Next
        End Sub
        Private Function killThread(threadName As String) As Boolean
            Dim proc = Process.GetProcesses()
            Dim foundCamera As Boolean
            For i = 0 To proc.Count - 1
                If proc(i).ProcessName.ToLower.Contains(threadName) Then
                    Try
                        If proc(i).HasExited = False Then
                            proc(i).Kill()
                            If proc(i).ProcessName.ToLower.Contains(threadName) Then
                                Thread.Sleep(100) ' let the camera task free resources.
                                foundCamera = True
                            End If
                        End If
                    Catch ex As Exception
                    End Try
                End If
                If proc(i).ProcessName.ToLower.Contains("translator") Then
                    If proc(i).HasExited = False Then proc(i).Kill()
                End If
            Next
            Return foundCamera
        End Function
        Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs) Handles RefreshTimer.Tick
            If AvailableAlgorithms.Items.Count = 0 Then Exit Sub
            If (paintNewImages Or algorithmRefresh) And AvailableAlgorithms.Text.StartsWith(saveAlgorithmName) Then
                camPic(0).Refresh()
                camPic(1).Refresh()
                camPic(2).Refresh()
                camPic(3).Refresh()
            End If
        End Sub
        Private Sub PixelViewerButton_Click(sender As Object, e As EventArgs) Handles PixelViewerButton.Click
            PixelViewerButton.Checked = Not PixelViewerButton.Checked
            pixelViewerOn = PixelViewerButton.Checked
        End Sub
        Private Sub setupAlgorithmHistory()
            For i = 0 To MAX_RECENT - 1
                Dim nextA = GetSetting("OpenCVB", "algHistory" + CStr(i), "algHistory" + CStr(i), "recent algorithm " + CStr(i))
                If nextA = "" Then Exit For
                If algHistory.Contains(nextA) = False Then
                    algHistory.Add(nextA)
                    RecentList.DropDownItems.Add(nextA)
                    AddHandler RecentList.DropDownItems(RecentList.DropDownItems.Count - 1).Click, AddressOf algHistory_Clicked
                End If
            Next
        End Sub
        Private Sub StartAlgorithm()
            Debug.WriteLine("")
            Debug.WriteLine("")
            Debug.WriteLine("Starting algorithm " + AvailableAlgorithms.Text)
            Debug.WriteLine(vbTab + CStr(AlgorithmTestAllCount) + " algorithms tested")
            testAllRunning = TestAllButton.Text = "Stop Test"
            saveAlgorithmName = AvailableAlgorithms.Text ' this tells the previous algorithmTask to terminate.

            Dim parms As New VBClasses.VBtask.algParms
            parms.fpsRate = settings.desiredFPS

            parms.testAllRunning = testAllRunning

            parms.showConsoleLog = settings.showConsoleLog

            parms.HomeDir = HomeDir.FullName
            parms.cameraName = settings.cameraName
            parms.cameraIndex = settings.cameraIndex

            parms.main_hwnd = Me.Handle
            parms.mainFormLocation = New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height)

            parms.workRes = settings.workRes
            parms.captureRes = settings.captureRes
            parms.algName = AvailableAlgorithms.Text

            PausePlayButton.Image = PausePlay

            If parms.algName = "GL_MainForm" Then
                GLControl.Visible = True
                GLControl.Location = camPic(2).Location
                GLControl.Width = camPic(2).Width
                GLControl.Height = camPic(2).Height
            Else
                GLControl.Visible = False
            End If

            GC.Collect()

            Thread.CurrentThread.Priority = ThreadPriority.Lowest
            algorithmTaskHandle = New Thread(AddressOf AlgorithmTask) ' <<<<<<<<<<<<<<<<<<<<<<<<< This starts the VB_Classes algorithm.
            AlgDescription.Text = ""
            algorithmTaskHandle.Name = AvailableAlgorithms.Text
            algorithmTaskHandle.SetApartmentState(ApartmentState.STA) ' this allows the algorithm task to display forms and react to input.
            algorithmTaskHandle.Start(parms)
        End Sub
    End Class
End Namespace