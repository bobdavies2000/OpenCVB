﻿Imports System.Threading
Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports VB_Classes
Imports System.Management
Imports cvext = OpenCvSharp.Extensions
Module opencv_module
    ' Public bufferLock As New Mutex(True, "bufferLock") ' this is a global lock on the camera buffers.
    Public callTraceLock As New Mutex(True, "callTraceLock")
    Public mouseLock As New Mutex(True, "mouseLock") ' global lock for use with mouse clicks.
    Public algorithmThreadLock As New Mutex(True, "AlgorithmThreadLock")
    Public cameraLock As New Mutex(True, "cameraLock")
    Public Declare Function GetWindowRect Lib "user32" (ByVal HWND As Integer, ByRef lpRect As RECT) As Integer
    <StructLayout(LayoutKind.Sequential)> Public Structure RECT
        Dim Left As Integer
        Dim Top As Integer
        Dim Right As Integer
        Dim Bottom As Integer
    End Structure
End Module


Public Class OpenCVB_UI
#Region "Globals"
    Dim threadStartTime As DateTime

    Dim optionsForm As OptionsDialog
    Dim AlgorithmCount As Integer
    Dim AlgorithmTestAllCount As Integer
    Dim algorithmTaskHandle As Thread
    Dim algorithmQueueCount As Integer

    Dim saveAlgorithmName As String
    Dim shuttingDown As Boolean

    Dim BothFirstAndLastReady As Boolean

    Dim camera As Object
    Dim restartCameraRequest As Boolean

    Dim cameraTaskHandle As Thread
    Dim camPic(4 - 1) As PictureBox
    Dim camLabel(camPic.Count - 1) As Label
    Dim dst(camPic.Count - 1) As cv.Mat

    Dim paintNewImages As Boolean
    Dim newCameraImages As Boolean

    Dim algorithmRefresh As Boolean
    Dim CodeLineCount As Integer
    Dim DrawingRectangle As Boolean
    Dim drawRect As New cv.Rect
    Dim drawRectPic As Integer
    Dim externalPythonInvocation As Boolean
    Dim frameCount As Integer
    Dim GrabRectangleData As Boolean
    Public HomeDir As DirectoryInfo

    Dim LastX As Integer
    Dim LastY As Integer
    Dim mouseClickFlag As Boolean
    Dim ClickPoint As New cv.Point
    Dim mousePicTag As Integer
    Dim mouseDownPoint As New cv.Point
    Dim mouseMovePoint As New cv.Point
    Dim mousePoint As New cv.Point
    Dim activeMouseDown As Boolean

    Dim myBrush = New SolidBrush(Color.White)
    Dim groupNames As New List(Of String)
    Dim TreeViewDialog As TreeviewForm
    Public algorithmFPS As Single
    Dim picLabels() = {"", "", "", ""}
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen
    Dim textDesc As String = ""
    Dim textAdvice As String = ""
    Dim totalBytesOfMemoryUsed As Integer
    Dim trueData As New List(Of VB_Classes.trueText)
    Dim mbuf(2 - 1) As VB_Classes.VBtask.inBuffer
    Dim mbIndex As Integer

    Dim pauseAlgorithmThread As Boolean
    Dim logAlgorithms As StreamWriter
    Public callTrace As New List(Of String)
    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)

    Const MAX_RECENT = 50
    Dim algHistory As New List(Of String)
    Dim arrowList As New List(Of String)
    Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
    Dim arrowIndex As Integer

    Public intermediateReview As String
    Dim activateBlocked As Boolean
    Dim activateTaskRequest As Boolean
    Dim activateTreeView As Boolean
    Dim pixelViewerRect As cv.Rect
    Dim pixelViewTag As Integer

    Dim PausePlay As Bitmap
    Dim runPlay As Bitmap
    Dim stopTest As Bitmap
    Dim complexityTest As Bitmap
    Dim complexityResults As New List(Of String)
    Dim complexityStartTime As DateTime

    Dim testAllToolbarBitmap As Bitmap

    Public testAllRunning As Boolean

    Public colorTransitionCount As Integer
    Public colorScheme As String
    Dim pythonPresent As Boolean

    Dim testAllResolutionCount As Integer
    Dim testAllStartingRes As Integer
    Dim testAllEndingRes As Integer
    Dim windowsVersion As Integer
#End Region
    Public Shared settings As jsonClass.ApplicationStorage
    Public Shared cameraNames As List(Of String)
    Dim jsonfs As New jsonClass.FileOperations
    Dim upArrow As Boolean, downArrow As Boolean
    Public Sub jsonRead()
        jsonfs.jsonFileName = HomeDir.FullName + "settings.json"
        settings = jsonfs.Load()(0)

        cameraNames = New List(Of String)(VB_Classes.VBtask.algParms.cameraNames)
        With settings
            .cameraSupported = New List(Of Boolean)({True, True, True, True, True, False, True}) ' Zed and Mynt updated below if supported
            .camera640x480Support = New List(Of Boolean)({False, True, True, False, False, False, True})
            .camera1920x1080Support = New List(Of Boolean)({True, False, False, False, True, False, False})
            Dim defines = New FileInfo(HomeDir.FullName + "Cameras\CameraDefines.hpp")
            Dim sr = New StreamReader(defines.FullName)
            If Trim(sr.ReadLine).StartsWith("//#define STEREOLAB_INSTALLED") = False Then .cameraSupported(4) = True
            If Trim(sr.ReadLine).StartsWith("//#define MYNTD_1000") = False Then .cameraSupported(5) = True
            sr.Close()

            .cameraPresent = New List(Of Boolean)
            For i = 0 To cameraNames.Count - 1
                Dim present = USBenumeration(cameraNames(i))
                If cameraNames(i).Contains("Orbbec") Then present = USBenumeration("Orbbec Gemini 335L Depth Camera")
                If cameraNames(i).Contains("Oak-D") Then present = USBenumeration("Movidius MyriadX")
                If cameraNames(i).Contains("StereoLabs ZED 2/2i") Then present = USBenumeration("ZED 2i")
                If present = False And cameraNames(i).Contains("StereoLabs ZED 2/2i") Then present = USBenumeration("ZED 2") ' older edition.
                .cameraPresent.Add(present <> 0)
            Next

            For i = 0 To cameraNames.Count - 1
                If cameraNames(i).Contains(.cameraName) Then
                    .cameraIndex = i
                    Exit For
                End If
            Next

            If .cameraName = "" Or .cameraPresent(.cameraIndex) = False Then
                For i = 0 To cameraNames.Count - 1
                    If .cameraPresent(i) And .cameraSupported(i) Then
                        .cameraIndex = i
                        .cameraName = cameraNames(i)
                        Exit For
                    End If
                Next
            Else
                For i = 0 To cameraNames.Count - 1
                    If cameraNames(i) = .cameraName Then .cameraIndex = i
                Next
            End If

            Dim myntIndex = cameraNames.IndexOf("MYNT-EYE-D1000")
            If .cameraPresent(myntIndex) And .cameraSupported(myntIndex) = False Then
                'MsgBox("A MYNT D 1000 camera is present but OpenCVB's" + vbCrLf +
                '   "Cam_MyntD.dll has not been built." + vbCrLf + vbCrLf +
                '   "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                '   "and run AddMynt.bat in OpenCVB's home directory.")
            End If

            Dim zedIndex = cameraNames.IndexOf("StereoLabs ZED 2/2i")
            If .cameraPresent(zedIndex) And .cameraSupported(zedIndex) = False Then
                MsgBox("A StereoLabls ZED 2 camera is present but OpenCVB's" + vbCrLf +
                       "Cam_Zed2.dll has not been built with the SDK." + vbCrLf + vbCrLf +
                       "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                       "and rebuild OpenCVB with the StereoLabs SDK.")
            End If


            settings.cameraFound = False
            For i = 0 To settings.cameraPresent.Count - 1
                If settings.cameraPresent(i) Then
                    settings.cameraFound = True
                    Exit For
                End If
            Next
            If settings.cameraFound = False Then
                settings.cameraName = ""
                MsgBox("There are no supported cameras present!" + vbCrLf + vbCrLf)
            End If

            If settings.testAllDuration < 5 Then settings.testAllDuration = 5
            If settings.fontInfo Is Nothing Then settings.fontInfo = New Font("Tahoma", 9)

            Select Case .WorkingRes.Height
                Case 270, 540, 1080
                    .captureRes = New cv.Size(1920, 1080)
                    If .camera1920x1080Support(.cameraIndex) = False Then
                        .captureRes = New cv.Size(1280, 720)
                        .WorkingRes = New cv.Size(320, 180)
                    End If
                Case 180, 360, 720
                    .captureRes = New cv.Size(1280, 720)
                Case 376, 188, 94
                    .captureRes = New cv.Size(672, 376)
                Case 120, 240, 480
                    .captureRes = New cv.Size(640, 480)
                    If .camera640x480Support(.cameraIndex) = False Then
                        .captureRes = New cv.Size(1280, 720)
                        .WorkingRes = New cv.Size(320, 180)
                    End If
            End Select

            Dim wh = .WorkingRes.Height
            ' desktop style is the default
            If .snap320 = False And .snap640 = False And .snapCustom = False Then .snap640 = True
            If .snap640 Then
                .locationMain.Item2 = 1321
                .locationMain.Item3 = 858
                If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 1096
                If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cv.Size(640, 480) Else .displayRes = New cv.Size(640, 360)
            ElseIf .snap320 Then
                .locationMain.Item2 = 683
                .locationMain.Item3 = 500
                If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 616
                If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cv.Size(320, 240) Else .displayRes = New cv.Size(320, 180)
            End If

            Dim border As Integer = 6
            Dim defaultWidth = .WorkingRes.Width * 2 + border * 7
            Dim defaultHeight = .WorkingRes.Height * 2 + ToolStrip1.Height + border * 12
            If Me.Height < 50 Then
                Me.Width = defaultWidth
                Me.Height = defaultHeight
            End If

            If .fontInfo Is Nothing Then .fontInfo = New Font("Tahoma", 9)
            If settings.algorithmGroup = "" Then settings.algorithmGroup = "<All but Python"

            If testAllRunning = False Then
                Dim resStr = CStr(.WorkingRes.Width) + "x" + CStr(.WorkingRes.Height)
                For i = 0 To OptionsDialog.resolutionList.Count - 1
                    If OptionsDialog.resolutionList(i).StartsWith(resStr) Then
                        .WorkingResIndex = i
                        Exit For
                    End If
                Next
            End If

            .desiredFPS = 60
            Me.Left = .locationMain.Item0
            Me.Top = .locationMain.Item1
            Me.Width = .locationMain.Item2
            Me.Height = .locationMain.Item3
            optionsForm = New OptionsDialog
            optionsForm.defineCameraResolutions(settings.cameraIndex)
        End With
    End Sub
    Public Sub jsonWrite()
        If TestAllButton.Text <> "Stop Test" Then ' don't save the algorithm name and group if testing all
            settings.algorithm = AvailableAlgorithms.Text
            settings.algorithmGroup = GroupName.Text
        End If

        settings.locationMain = New cv.Vec4f(Me.Left, Me.Top, Me.Width, Me.Height)
        settings.treeButton = TreeButton.Checked
        settings.PixelViewerButton = False
        settings.displayRes = New cv.Size(camPic(0).Width, camPic(0).Height) ' used only when .snapCustom is true

        Dim setlist = New List(Of jsonClass.ApplicationStorage)
        setlist.Add(settings)
        jsonfs.Save(setlist)
    End Sub
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        threadStartTime = DateTime.Now

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture
        Dim args() = Environment.GetCommandLineArgs()

        HomeDir = If(args.Length > 1, New DirectoryInfo(CurDir() + "\..\"), New DirectoryInfo(CurDir() + "\..\..\"))


        AlgorithmDesc.Top = ToolStrip1.Top
        AlgorithmDesc.Width = ToolStrip1.Left + ToolStrip1.Width - AlgorithmDesc.Left
        AlgorithmDesc.Text = "This is a test of the description field in the output of OpenCVB.  It is not meant to mean anything.  Just fill the label window."
        AvailableAlgorithms.Width = 500
        AvailableAlgorithms.Items.Add("This is a test of autosize")
        AvailableAlgorithms.SelectedIndex = 0
        AvailableAlgorithms.ComboBox.TabIndex = 0
    End Sub
    Public Function USBenumeration(searchName As String) As Boolean
        Static usblist As New List(Of String)
        Dim info As ManagementObject
        Dim search As ManagementObjectSearcher
        search = New ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
        If usblist.Count = 0 Then
            For Each info In search.Get()
                Dim Name = CType(info("Caption"), String)
                If Name IsNot Nothing Then
                    usblist.Add(Name)
                    ' why do this?  So enumeration can tell us about the cameras present in a short list.
                    If InStr(Name, "Xeon") Or InStr(Name, "Chipset") Or InStr(Name, "Generic") Or InStr(Name, "Bluetooth") Or
                        InStr(Name, "Monitor") Or InStr(Name, "Mouse") Or InStr(Name, "NVIDIA") Or InStr(Name, "HID-compliant") Or
                        InStr(Name, " CPU ") Or InStr(Name, "PCI Express") Or Name.StartsWith("USB ") Or
                        Name.StartsWith("Microsoft") Or Name.StartsWith("Motherboard") Or InStr(Name, "SATA") Or
                        InStr(Name, "Volume") Or Name.StartsWith("WAN") Or InStr(Name, "ACPI") Or
                        Name.StartsWith("HID") Or InStr(Name, "OneNote") Or Name.StartsWith("Samsung") Or
                        Name.StartsWith("System ") Or Name.StartsWith("HP") Or InStr(Name, "Wireless") Or
                        Name.StartsWith("SanDisk") Or InStr(Name, "Wi-Fi") Or Name.StartsWith("Media ") Or
                        Name.StartsWith("High precision") Or Name.StartsWith("High Definition ") Or
                        InStr(Name, "Remote") Or InStr(Name, "Numeric") Or InStr(Name, "UMBus ") Or
                        Name.StartsWith("Plug or Play") Or InStr(Name, "Print") Or Name.StartsWith("Direct memory") Or
                        InStr(Name, "interrupt controller") Or Name.StartsWith("NVVHCI") Or Name.StartsWith("Plug and Play") Or
                        Name.StartsWith("ASMedia") Or Name = "Fax" Or Name.StartsWith("Speakers") Or
                        InStr(Name, "Host Controller") Or InStr(Name, "Management Engine") Or InStr(Name, "Legacy") Or
                        Name.StartsWith("NDIS") Or Name.StartsWith("Logitech USB Input Device") Or
                        Name.StartsWith("Simple Device") Or InStr(Name, "Ethernet") Or Name.StartsWith("WD ") Or
                        InStr(Name, "Composite Bus Enumerator") Or InStr(Name, "Turbo Boost") Or Name.StartsWith("Realtek") Or
                        Name.StartsWith("PCI-to-PCI") Or Name.StartsWith("Network Controller") Or Name.StartsWith("ATAPI ") Or
                        Name.Contains("Gen Intel(R) ") Then
                    Else
                        Console.WriteLine(Name) ' looking for new cameras 
                    End If
                End If
            Next
        End If
        For Each usbDevice In usblist
            If usbDevice.Contains(searchName) Then Return True
        Next
        Return False
    End Function
    Private Sub addNextAlgorithm(nextName As String, ByRef lastNameSplit As String)
        Dim nameSplit = nextName.Split("_")
        If nameSplit(0) <> lastNameSplit And lastNameSplit <> "" Then AvailableAlgorithms.Items.Add("")
        lastNameSplit = nameSplit(0)
        AvailableAlgorithms.Items.Add(nextName)
    End Sub
    Private Sub groupName_SelectedIndexChanged(sender As Object, e As EventArgs) Handles GroupName.SelectedIndexChanged
        If GroupName.Text = "" Then
            Dim incr = 1
            If upArrow Then incr = -1
            upArrow = False
            downArrow = False
            GroupName.Text = GroupName.Items(GroupName.SelectedIndex + incr)
            Exit Sub
        End If

        Dim lastNameSplit As String = ""
        If GroupName.Text.StartsWith("<All (") Or GroupName.Text = "<All using recorded data>" Then
            Dim AlgorithmListFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmList.txt")
            Dim sr = New StreamReader(AlgorithmListFileInfo.FullName)

            Dim infoLine = sr.ReadLine
            Dim Split = Regex.Split(infoLine, "\W+")
            CodeLineCount = Split(1)
            AvailableAlgorithms.Items.Clear()
            While sr.EndOfStream = False
                infoLine = sr.ReadLine
                infoLine = UCase(Mid(infoLine, 1, 1)) + Mid(infoLine, 2)
                If infoLine.StartsWith("Options_") = False Then
                    addNextAlgorithm(infoLine, lastNameSplit)
                End If
            End While
            sr.Close()
        Else
            AvailableAlgorithms.Enabled = False
            Dim keyIndex = GroupName.Items.IndexOf(GroupName.Text)
            Dim groupings = groupNames(keyIndex)
            Dim split = Regex.Split(groupings, ",")
            AvailableAlgorithms.Items.Clear()

            For i = 1 To split.Length - 1
                If split(i).StartsWith("Options_") = False Then
                    addNextAlgorithm(split(i), lastNameSplit)
                End If
            Next
            AvailableAlgorithms.Enabled = True
        End If
        If GroupName.Text.Contains("All") = False Then algHistory.Clear()

        ' if the fpstimer is enabled, then OpenCVB is running - not initializing.
        If fpsTimer.Enabled Then
            If AvailableAlgorithms.Items.Contains(settings.algorithm) Then
                AvailableAlgorithms.Text = settings.algorithm
            Else
                AvailableAlgorithms.SelectedIndex = 0
            End If
        End If
    End Sub
    Private Sub OpenCVB_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        jsonWrite()
    End Sub
    Private Sub OpenCVB_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If camPic Is Nothing Then Exit Sub ' when first opening, campic may not be built yet
        If camPic(2) Is Nothing Then Exit Sub ' individual pictureboxes need to be ready as well.
        LineUpCamPics()
    End Sub
    Private Sub LineUpCamPics()
        Dim height = settings.displayRes.Height
        If settings.snap640 Then width = 640
        If settings.snap320 Then width = 320
        If settings.snapCustom Then ' custom size - neither snap320 or snap640
            Dim ratio = settings.WorkingRes.Height / settings.WorkingRes.Width
            height = CInt(width * ratio)
        End If
        Dim padX = 12
        Dim padY = 60
        camPic(0).Size = New Size(width, height)
        camPic(1).Size = New Size(width, height)
        camPic(2).Size = New Size(width, height)
        camPic(3).Size = New Size(width, height)

        camPic(0).Image = New Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(1).Image = New Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(2).Image = New Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(3).Image = New Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(0).Location = New Point(padX, padY + camLabel(0).Height)
        camPic(1).Location = New Point(camPic(0).Left + camPic(0).Width, padY + camLabel(0).Height)
        camPic(2).Location = New Point(padX, camPic(0).Top + camPic(0).Height + camLabel(0).Height)
        camPic(3).Location = New Point(camPic(1).Left, camPic(2).Top)

        camLabel(0).Location = New Point(padX, padY)
        camLabel(1).Location = New Point(padX + camPic(0).Width, padY)
        camLabel(2).Location = New Point(padX, padY + camPic(0).Height + camLabel(0).Height)
        camLabel(3).Location = New Point(padX + camPic(0).Width, padY + camPic(0).Height + camLabel(0).Height)

        'Static saveAAwidth = AvailableAlgorithms.Width
        'Static saveKeyLeft = GroupName.Left
        'If AvailableAlgorithms.Left + AvailableAlgorithms.Width + GroupName.Width > Me.Width Then
        '    AvailableAlgorithms.Width = (Me.Width - AvailableAlgorithms.Left) / 2
        '    GroupName.Left = AvailableAlgorithms.Left + AvailableAlgorithms.Width + 1
        'ElseIf Me.Width > AvailableAlgorithms.Left + AvailableAlgorithms.Width * 2 Then
        '    AvailableAlgorithms.Width = saveAAwidth
        '    GroupName.Left = saveKeyLeft
        'End If

        XYLoc.Location = New Point(camPic(2).Left, camPic(2).Top + camPic(2).Height)
    End Sub
    Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
        Static lastAlgorithmFrame As Integer
        Static lastCameraFrame As Integer

        'Dim processThreads As ProcessThreadCollection = Process.GetCurrentProcess().Threads
        'Dim threadCount As Integer
        'For Each thread As ProcessThread In processThreads
        '    If thread.StartTime > threadStartTime Then threadCount += 1
        'Next

        'Console.WriteLine("Thread count = " + CStr(threadCount))

        If TreeButton.Checked And activateTreeView And activateBlocked = False Then
            TreeViewDialog.Activate()
            Me.Activate()
            activateTreeView = False
        End If

        If camera Is Nothing Then Exit Sub
        If lastAlgorithmFrame > frameCount Then lastAlgorithmFrame = 0
        If lastCameraFrame > camera.cameraFrameCount Then lastCameraFrame = 0
        If TreeViewDialog IsNot Nothing Then
            If TreeViewDialog.TreeView1.IsDisposed Then TreeButton.CheckState = CheckState.Unchecked
        End If

        If pauseAlgorithmThread = False Then

            Dim countFrames = frameCount - lastAlgorithmFrame
            lastAlgorithmFrame = frameCount
            algorithmFPS = countFrames / (fpsTimer.Interval / 1000)

            Dim camFrames = camera.cameraFrameCount - lastCameraFrame
            lastCameraFrame = camera.cameraFrameCount
            Dim cameraFPS As Single = camFrames / (fpsTimer.Interval / 1000)
            If cameraTaskHandle Is Nothing Then Exit Sub
            Dim cameraName = settings.cameraName
            cameraName = cameraName.Replace(" 2/2i", "")
            cameraName = cameraName.Replace(" camera", "")
            cameraName = cameraName.Replace(" Camera", "")
            cameraName = cameraName.Replace("Intel(R) RealSense(TM) Depth ", "Intel D")

            Me.Text = "OpenCVB - " + Format(CodeLineCount, "###,##0") + " lines / " + CStr(AlgorithmCount) + " algorithms = " +
                      CStr(CInt(CodeLineCount / AlgorithmCount)) + " lines each (avg) - " + cameraName +
                      " - Camera FPS: " + Format(cameraFPS, "0.0") + ", task FPS: " + Format(algorithmFPS, "0.0")
        End If
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
            PausePlayButton.Text = "Run"
            pauseAlgorithmThread = True
            saveTestAllState = TestAllTimer.Enabled
            If TestAllTimer.Enabled Then TestAllButton_Click(sender, e)
            PausePlayButton.Image = runPlay
        End If
    End Sub
    Private Sub Options_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
        If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
        Dim saveCameraIndex = settings.cameraIndex

        optionsForm.OptionsDialog_Load(sender, e)
        optionsForm.cameraRadioButton(settings.cameraIndex).Checked = True
        Dim resStr = CStr(settings.WorkingRes.Width) + "x" + CStr(settings.WorkingRes.Height)
        For i = 0 To OptionsDialog.resolutionList.Count - 1
            If OptionsDialog.resolutionList(i).StartsWith(resStr) Then
                optionsForm.WorkingResRadio(i).Checked = True
            End If
        Next

        Dim OKcancel = optionsForm.ShowDialog()

        If OKcancel = DialogResult.OK Then
            If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e)
            restartCameraRequest = True
            saveAlgorithmName = ""
            settings.WorkingRes = optionsForm.cameraWorkingRes
            settings.displayRes = optionsForm.cameraDisplayRes
            settings.cameraName = optionsForm.cameraName
            settings.cameraIndex = optionsForm.cameraIndex
            settings.testAllDuration = optionsForm.testDuration

            setupCamPics()

            jsonWrite()
            jsonRead() ' this will apply all the changes...

            StartTask()
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
            Console.WriteLine("Error in camPic_MouseUp: " + ex.Message)
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
            Console.WriteLine("Error in camPic_MouseDown: " + ex.Message)
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
            mousePoint.X = e.X
            mousePoint.Y = e.Y
            mousePoint *= settings.WorkingRes.Width / camPic(0).Width
            XYLoc.Text = mousePoint.ToString + " - last click point at: " + ClickPoint.ToString
        Catch ex As Exception
            Console.WriteLine("Error in camPic_MouseMove: " + ex.Message)
        End Try
    End Sub
    Private Sub Campic_Click(sender As Object, e As EventArgs)
        SyncLock mouseLock
            mouseClickFlag = True
        End SyncLock
    End Sub
    Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
        DrawingRectangle = False
    End Sub
    Private Sub setupCamPics()
        Dim goodPoint = Screen.GetWorkingArea(New Point(Me.Left, Me.Top)) ' when they change the primary monitor, old coordinates can go way off the screen.
        If goodPoint.X > Me.Left Then Me.Left = goodPoint.X
        If goodPoint.Y > Me.Top Then Me.Top = goodPoint.Y

        trueData = New List(Of VB_Classes.trueText)

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
                AddHandler camPic(i).Click, AddressOf campic_Click
                AddHandler camPic(i).Paint, AddressOf campic_Paint
                AddHandler camPic(i).MouseDown, AddressOf camPic_MouseDown
                AddHandler camPic(i).MouseUp, AddressOf camPic_MouseUp
                AddHandler camPic(i).MouseMove, AddressOf camPic_MouseMove
                camPic(i).Tag = i
                camPic(i).Size = New Size(settings.WorkingRes.Width, settings.WorkingRes.Height)
                Me.Controls.Add(camPic(i))
            Next
        End If
        LineUpCamPics()
    End Sub

    Private Sub TestAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
        TestAllButton.Image = If(TestAllButton.Text = "Test All", stopTest, testAllToolbarBitmap)
        If TestAllButton.Text = "Test All" Then
            AlgorithmTestAllCount = 0

            setupTestAll()
            AlgorithmTestAllCount = 1

            TestAllButton.Text = "Stop Test"
            AvailableAlgorithms.Enabled = False  ' the algorithm will be started in the testAllTimer event.
            jsonWrite()
            jsonRead()
            TestAllTimer.Interval = 1
            TestAllTimer.Enabled = True
        Else
            AvailableAlgorithms.Enabled = True
            TestAllTimer.Enabled = False
            TestAllButton.Text = "Test All"
        End If
    End Sub

    Private Sub BluePlusButton_Click(sender As Object, e As EventArgs) Handles BluePlusButton.Click
        Dim OKcancel = InsertAlgorithm.ShowDialog()
    End Sub

    Private Sub ComplexityTimer_Tick(sender As Object, e As EventArgs) Handles ComplexityTimer.Tick
        While 1
            If OpenCVB_UI.settings.resolutionsSupported(settings.WorkingResIndex) Then
                setWorkingRes()
                Exit While
            Else
                settings.WorkingResIndex -= 1
                If settings.WorkingResIndex < 0 Then
                    settings.WorkingResIndex = OpenCVB_UI.settings.resolutionsSupported.Count - 1
                End If
            End If
        End While

        If complexityResults.Count > 0 Then
            Dim endTime = Now
            Dim span As TimeSpan = endTime - complexityStartTime
            complexityResults.Add("Ending " + vbTab + CStr(frameCount) + vbTab +
                                  Format(span.TotalMilliseconds / 1000, "0.000") + " seconds")
            complexityStartTime = Now
        End If
        complexityResults.Add("-------------------")
        complexityResults.Add("Image" + vbTab + CStr(settings.WorkingRes.Width) + vbTab +
                                      CStr(settings.WorkingRes.Height))
        jsonWrite()
        jsonRead()
        LineUpCamPics()

        ' when switching resolution, best to reset these as the move from higher to lower res
        ' could mean the point is no longer valid.
        ClickPoint = New cv.Point
        mousePoint = New cv.Point

        StartTask()

        settings.WorkingResIndex -= 1
        If settings.WorkingResIndex < 0 Then
            settings.WorkingResIndex = OpenCVB_UI.settings.resolutionsSupported.Count - 1
        End If
    End Sub
    Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
        ' don't start another algorithm until the current one has finished 
        If algorithmQueueCount <> 0 Then
            Console.WriteLine("Can't start the next algorithm because previous algorithm has not completed.")
            While 1
                If algorithmQueueCount = 0 Then Exit While
                'Console.Write(".")
                Application.DoEvents()
            End While
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

        TestAllTimer.Interval = settings.testAllDuration * 1000
        Static startingAlgorithm = AvailableAlgorithms.Text
        If AvailableAlgorithms.Text = startingAlgorithm And AlgorithmTestAllCount > 1 Then
            If settings.WorkingResIndex > testAllEndingRes Then
                While 1
                    settings.cameraIndex += 1
                    If settings.cameraIndex >= cameraNames.Count - 1 Then settings.cameraIndex = 0
                    settings.cameraName = cameraNames(settings.cameraIndex)
                    If settings.cameraPresent(settings.cameraIndex) Then
                        OptionsDialog.defineCameraResolutions(settings.cameraIndex)
                        setupTestAll()
                        settings.WorkingResIndex = testAllStartingRes
                        Exit While
                    End If
                End While
                ' extra time for the camera to restart...
                TestAllTimer.Interval = settings.testAllDuration * 1000 * 3
            End If

            setWorkingRes()

            jsonWrite()
            jsonRead()
            LineUpCamPics()

            ' when switching resolution, best to reset these as the move from higher to lower res
            ' could mean the point is no longer valid.
            ClickPoint = New cv.Point
            mousePoint = New cv.Point
        End If

        Static saveLastAlgorithm = AvailableAlgorithms.Text
        If saveLastAlgorithm <> AvailableAlgorithms.Text Then
            settings.WorkingResIndex += 1
            saveLastAlgorithm = AvailableAlgorithms.Text
        End If
        StartTask()
    End Sub
    Private Sub StartTask()
        Console.WriteLine("Starting algorithm " + AvailableAlgorithms.Text)
        SyncLock callTraceLock
            If TreeViewDialog IsNot Nothing Then
                callTrace.Clear()
                algorithm_ms.Clear()
                algorithmNames.Clear()
                TreeViewDialog.PercentTime.Text = ""
            End If
        End SyncLock
        testAllRunning = TestAllButton.Text = "Stop Test"
        saveAlgorithmName = AvailableAlgorithms.Text ' this tells the previous algorithmTask to terminate.

        Dim parms As New VB_Classes.VBtask.algParms
        parms.fpsRate = settings.desiredFPS

        parms.useRecordedData = GroupName.Text = "<All using recorded data>"
        parms.testAllRunning = testAllRunning

        parms.externalPythonInvocation = externalPythonInvocation
        parms.showConsoleLog = settings.showConsoleLog

        parms.HomeDir = HomeDir.FullName
        parms.cameraName = settings.cameraName
        parms.cameraIndex = settings.cameraIndex
        If settings.cameraName <> "" Then parms.cameraInfo = camera.cameraInfo

        parms.main_hwnd = Me.Handle
        parms.mainFormLocation = New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height)

        parms.WorkingRes = settings.WorkingRes
        parms.captureRes = settings.captureRes
        parms.displayRes = settings.displayRes
        parms.algName = AvailableAlgorithms.Text

        PausePlayButton.Image = PausePlay

        ' If they Then had been Using the treeview feature To click On a tree entry, the timer was disabled.  
        ' Clicking on availablealgorithms indicates they are done with using the treeview.
        If TreeViewDialog IsNot Nothing Then TreeViewDialog.TreeViewTimer.Enabled = True

        Thread.CurrentThread.Priority = ThreadPriority.Lowest
        algorithmTaskHandle = New Thread(AddressOf AlgorithmTask) ' <<<<<<<<<<<<<<<<<<<<<<<<< This starts the VB_Classes algorithm.

        algorithmTaskHandle.Name = AvailableAlgorithms.Text
        algorithmTaskHandle.SetApartmentState(ApartmentState.STA) ' this allows the algorithm task to display forms and react to input.
        algorithmTaskHandle.Start(parms)
        Console.WriteLine("Start Algorithm completed.")
    End Sub
    Private Sub AlgorithmTask(ByVal parms As VB_Classes.VBtask.algParms)
        If parms.algName = "" Then Exit Sub
        algorithmQueueCount += 1
        ' the duration of any algorithm varies a lot so wait here if previous algorithm is not finished.
        SyncLock algorithmThreadLock
            algorithmQueueCount -= 1
            AlgorithmTestAllCount += 1
            drawRect = New cv.Rect
            Dim task = New VBtask(parms)
            textDesc = task.desc
            intermediateReview = ""

            If ComplexityTimer.Enabled = False Then
                Console.WriteLine(CStr(Now))
                Console.WriteLine(vbCrLf + vbCrLf + vbTab + parms.algName + vbCrLf + vbTab +
                                  CStr(AlgorithmTestAllCount) + vbTab + "Algorithms tested")
                Console.WriteLine(vbTab + Format(totalBytesOfMemoryUsed, "#,##0") + "Mb working set before running " +
                                  parms.algName + " with " + CStr(Process.GetCurrentProcess().Threads.Count) + " threads")

                Console.WriteLine(vbTab + "Active camera = " + settings.cameraName + ", Input resolution " +
                                  CStr(settings.captureRes.Width) + "x" + CStr(settings.captureRes.Height) + " and working resolution of " +
                                  CStr(settings.WorkingRes.Width) + "x" + CStr(settings.WorkingRes.Height) + vbCrLf)
            End If
            ' Adjust drawrect for the ratio of the actual size and WorkingRes.
            If task.drawRect <> New cv.Rect Then
                ' relative size of algorithm size image to displayed image
                Dim ratio = camPic(0).Width / task.WorkingRes.Width
                drawRect = New cv.Rect(task.drawRect.X * ratio, task.drawRect.Y * ratio,
                                       task.drawRect.Width * ratio, task.drawRect.Height * ratio)
            End If

            Dim saveWorkingRes = settings.WorkingRes
            task.labels = {"", "", "", ""}
            mousePoint = New cv.Point(task.WorkingRes.Width / 2, task.WorkingRes.Height / 2) ' mouse click point default = center of the image

            Dim saveDrawRect As cv.Rect
            While 1
                Dim waitTime = Now
                ' relative size of displayed image and algorithm size image.
                While 1
                    ' camera has exited or resolution is changed.
                    If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Or
                    saveWorkingRes <> settings.WorkingRes Then Exit While
                    If saveAlgorithmName <> task.algName Then Exit While
                    ' switching camera resolution means stopping the current algorithm
                    If saveWorkingRes <> settings.WorkingRes Then Exit While

                    If pauseAlgorithmThread Then
                        task.paused = True
                        Exit While ' this is useful because the pixelviewer can be used if paused.
                    Else
                        task.paused = False
                    End If

                    If newCameraImages Then
                        Dim copyTime = Now

                        task.color = mbuf(mbIndex).color
                        task.leftView = mbuf(mbIndex).leftView
                        task.rightView = mbuf(mbIndex).rightView
                        task.pointCloud = mbuf(mbIndex).pointCloud

                        If frameCount < 10 Then
                            Dim sizeRatio = settings.captureRes.Width / saveWorkingRes.Width
                            task.calibData.ppx = task.WorkingRes.Width / 2 ' camera.cameraInfo.ppx / sizeRatio
                            task.calibData.ppy = task.WorkingRes.Height / 2 ' camera.cameraInfo.ppy / sizeRatio
                            task.calibData.fx = camera.cameraInfo.fx
                            task.calibData.fy = camera.cameraInfo.fy
                            task.calibData.v_fov = camera.cameraInfo.v_fov
                            task.calibData.h_fov = camera.cameraInfo.h_fov
                            task.calibData.d_fov = camera.cameraInfo.d_fov
                        End If
                        SyncLock cameraLock
                            task.mbuf(mbIndex) = mbuf(mbIndex)
                            task.mbIndex = mbIndex
                            mbIndex += 1
                            If mbIndex >= mbuf.Count Then mbIndex = 0

                            task.transformationMatrix = camera.transformationMatrix
                            task.IMU_TimeStamp = camera.IMU_TimeStamp
                            task.IMU_Acceleration = camera.IMU_Acceleration
                            task.IMU_AngularAcceleration = camera.IMU_AngularAcceleration
                            task.IMU_AngularVelocity = camera.IMU_AngularVelocity
                            task.IMU_FrameTime = camera.IMU_FrameTime
                            task.CPU_TimeStamp = camera.CPU_TimeStamp
                            task.CPU_FrameTime = camera.CPU_FrameTime
                        End SyncLock

                        task.activateTaskRequest = activateTaskRequest
                        activateTaskRequest = False

                        Dim endCopyTime = Now
                        Dim elapsedCopyTicks = endCopyTime.Ticks - copyTime.Ticks
                        Dim spanCopy = New TimeSpan(elapsedCopyTicks)
                        task.inputBufferCopy = spanCopy.Ticks / TimeSpan.TicksPerMillisecond

                        If intermediateReview = task.algName Then
                            task.intermediateName = ""
                            intermediateReview = ""
                        End If
                        If intermediateReview <> "" Then task.intermediateName = intermediateReview

                        newCameraImages = False

                        task.pixelViewerOn = If(testAllRunning, False, settings.PixelViewerButton)

                        If GrabRectangleData Then
                            GrabRectangleData = False
                            ' relative size of algorithm size image to displayed image
                            Dim ratio = task.WorkingRes.Width / camPic(0).Width
                            Dim tmpDrawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                            task.drawRect = New cv.Rect
                            If tmpDrawRect.Width > 0 And tmpDrawRect.Height > 0 Then
                                If saveDrawRect <> tmpDrawRect Then
                                    task.optionsChanged = True
                                    saveDrawRect = tmpDrawRect
                                End If
                                task.drawRect = tmpDrawRect
                            End If
                            BothFirstAndLastReady = False
                        End If

                        textAdvice = task.advice

                        If task.pointCloud.Width = 0 Then Continue While Else Exit While
                    End If
                End While

                ' camera has exited or resolution is changed.
                If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Or saveWorkingRes <> settings.WorkingRes Or
                saveAlgorithmName <> task.algName Then
                    Exit While
                End If

                If activeMouseDown = False Then
                    SyncLock mouseLock
                        If mousePoint.X < 0 Then mousePoint.X = 0
                        If mousePoint.Y < 0 Then mousePoint.Y = 0
                        If mousePoint.X >= task.WorkingRes.Width Then mousePoint.X = task.WorkingRes.Width - 1
                        If mousePoint.Y >= task.WorkingRes.Height Then mousePoint.Y = task.WorkingRes.Height - 1

                        task.mouseMovePoint = mousePoint
                        If task.mouseMovePoint = New cv.Point(0, 0) Then
                            task.mouseMovePoint = New cv.Point(task.WorkingRes.Width / 2, task.WorkingRes.Height / 2)
                        End If
                        task.mousePicTag = mousePicTag
                        If mouseClickFlag Then
                            task.mouseClickFlag = mouseClickFlag
                            task.ClickPoint = mousePoint
                            ClickPoint = task.ClickPoint
                            mouseClickFlag = False
                        End If
                    End SyncLock
                End If

                Dim endWaitTime = Now
                Dim elapsedWaitTicks = endWaitTime.Ticks - waitTime.Ticks
                Dim spanWait = New TimeSpan(elapsedWaitTicks)
                task.waitingForInput = spanWait.Ticks / TimeSpan.TicksPerMillisecond - task.inputBufferCopy
                Dim updatedDrawRect = task.drawRect
                If parms.algName.EndsWith("_CS") Then
                    Static findCSharp = New CS_Classes.CSAlgorithmList()
                    If task.csAlgorithmObject Is Nothing Then
                        task.csAlgorithmObject = findCSharp.createCSAlgorithm(parms.algName, task)
                        task.desc = task.csAlgorithmObject.desc
                    End If
                End If

                task.RunAlgorithm() ' <<<<<<<<<<<<<<<<<<<<<<<<< this is where the real work gets done.
                textDesc = task.desc
                picLabels = task.labels

                Dim returnTime = Now

                ' in case the algorithm has changed the mouse location...
                If task.mouseMovePointUpdated Then mousePoint = task.mouseMovePoint
                If updatedDrawRect <> task.drawRect Then
                    drawRect = task.drawRect
                    ' relative size of algorithm size image to displayed image
                    Dim ratio = camPic(0).Width / task.WorkingRes.Width
                    drawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                End If
                If task.drawRectClear Then
                    drawRect = New cv.Rect
                    task.drawRect = drawRect
                    task.drawRectClear = False
                End If

                pixelViewerRect = task.pixelViewerRect
                pixelViewTag = task.pixelViewTag

                task.fpsRate = If(algorithmFPS = 0, 1, algorithmFPS)

                picLabels = task.labels

                If task.paused = False Then
                    SyncLock trueData
                        If task.trueData.Count Then
                            trueData = New List(Of VB_Classes.trueText)(task.trueData)
                        Else
                            trueData = New List(Of VB_Classes.trueText)
                        End If
                        task.trueData.Clear()
                    End SyncLock
                End If

                If task.algName.StartsWith("Options_") Then
                    task.labels(2) = "Options algorithms have no output"
                    Continue While
                End If
                If task.dst0 IsNot Nothing Then
                    SyncLock cameraLock
                        dst(0) = task.dst0.Clone
                        dst(1) = task.dst1.Clone
                        dst(2) = task.dst2.Clone
                        dst(3) = task.dst3.Clone
                    End SyncLock
                    algorithmRefresh = True
                End If

                If frameCount Mod task.fpsRate = 0 Then
                    SyncLock callTraceLock
                        callTrace = New List(Of String)(task.callTraceMain)
                        algorithm_ms = New List(Of Single)(task.algorithm_msMain)
                        algorithmNames = New List(Of String)(task.algorithmNamesMain)
                    End SyncLock
                End If

                Dim elapsedTicks = Now.Ticks - returnTime.Ticks
                Dim span = New TimeSpan(elapsedTicks)
                task.returnCopyTime = span.Ticks / TimeSpan.TicksPerMillisecond

                task.mouseClickFlag = False
                frameCount = task.frameCount
                ' this can be very useful.  When debugging your algorithm, turn this global option on to sync output to debug.
                ' Each image will represent the one just finished by the algorithm.
                If task.debugSyncUI Then Thread.Sleep(100)
            End While

            Console.WriteLine(parms.algName + " ending.  Thread closing...")
            task.frameCount = -1
            Application.DoEvents()
            task.Dispose()
        End SyncLock

        frameCount = 0
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
    Private Sub AvailableAlgorithms_DropDown(sender As Object, e As EventArgs) Handles AvailableAlgorithms.DropDown
        activateBlocked = True
    End Sub
    Private Sub AvailableAlgorithms_DropDownClosed(sender As Object, e As EventArgs) Handles AvailableAlgorithms.DropDownClosed
        activateBlocked = False
    End Sub
    Private Sub GroupName_DropDown(sender As Object, e As EventArgs) Handles GroupName.DropDown
        activateBlocked = True
    End Sub
    Private Sub GroupName_DropDownClosed(sender As Object, e As EventArgs) Handles GroupName.DropDownClosed
        activateBlocked = False
    End Sub
    Private Sub setWorkingRes()
        Select Case settings.WorkingResIndex
            Case 0
                settings.WorkingRes = New cv.Size(1920, 1080)
                settings.captureRes = New cv.Size(1920, 1080)
            Case 1
                settings.WorkingRes = New cv.Size(960, 540)
                settings.captureRes = New cv.Size(1920, 1080)
            Case 2
                settings.WorkingRes = New cv.Size(480, 270)
                settings.captureRes = New cv.Size(1920, 1080)
            Case 3
                settings.WorkingRes = New cv.Size(1280, 720)
                settings.captureRes = New cv.Size(1280, 720)
            Case 4
                settings.WorkingRes = New cv.Size(640, 360)
                settings.captureRes = New cv.Size(1280, 720)
            Case 5
                settings.WorkingRes = New cv.Size(320, 180)
                settings.captureRes = New cv.Size(1280, 720)
            Case 6
                settings.WorkingRes = New cv.Size(640, 480)
                settings.captureRes = New cv.Size(640, 480)
            Case 7
                settings.WorkingRes = New cv.Size(320, 240)
                settings.captureRes = New cv.Size(640, 480)
            Case 8
                settings.WorkingRes = New cv.Size(160, 120)
                settings.captureRes = New cv.Size(640, 480)
            Case 9
                settings.WorkingRes = New cv.Size(672, 376)
                settings.captureRes = New cv.Size(672, 376)
            Case 10
                settings.WorkingRes = New cv.Size(336, 188)
                settings.captureRes = New cv.Size(672, 376)
            Case 11
                settings.WorkingRes = New cv.Size(168, 94)
                settings.captureRes = New cv.Size(672, 376)
        End Select
    End Sub

    Private Sub Advice_Click(sender As Object, e As EventArgs) Handles Advice.Click
        MsgBox(textAdvice)
    End Sub
    Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim pic = DirectCast(sender, PictureBox)
        Dim ratio = camPic(2).Width / settings.WorkingRes.Width
        g.ScaleTransform(1, 1)
        g.DrawImage(pic.Image, 0, 0)

        Static myWhitePen As New Pen(Color.White)
        Static myBlackPen As New Pen(Color.Black)

        If settings.PixelViewerButton And mousePicTag = pic.Tag Then
            Dim r = pixelViewerRect
            Dim rect = New cv.Rect(CInt(r.X * ratio), CInt(r.Y * ratio),
                                   CInt(r.Width * ratio), CInt(r.Height * ratio))
            g.DrawRectangle(myWhitePen, rect.X, rect.Y, rect.Width, rect.Height)
        End If

        If drawRect.Width > 0 And drawRect.Height > 0 Then
            g.DrawRectangle(myWhitePen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
            If pic.Tag = 2 Then
                g.DrawRectangle(myWhitePen, drawRect.X + camPic(0).Width, drawRect.Y, drawRect.Width, drawRect.Height)
            End If

        End If

        If paintNewImages Then
            paintNewImages = False
            Try
                SyncLock cameraLock
                    If camera.mbuf(mbIndex).color IsNot Nothing Then
                        If camera.mbuf(mbIndex).color.width > 0 And dst(0) IsNot Nothing Then
                            Dim camSize = New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height)
                            For i = 0 To dst.Count - 1
                                Dim tmp = dst(i).Resize(camSize)
                                cvext.BitmapConverter.ToBitmap(tmp, camPic(i).Image)
                            Next
                        End If
                    End If
                End SyncLock
            Catch ex As Exception
                Console.WriteLine("OpenCVB: Error in campic_Paint: " + ex.Message)
            End Try
        End If

        ' draw any TrueType font data on the image 
        SyncLock trueData
            Try
                For i = 0 To trueData.Count - 1
                    If trueData(i) Is Nothing Then Continue For
                    Dim tt = trueData(i)
                    If tt.text Is Nothing Then Continue For
                    ' campic(2) has both dst2 and dst3 to assure they are in sync.
                    If tt.text.Length > 0 And tt.picTag = pic.Tag Then
                        g.DrawString(tt.text, settings.fontInfo, New SolidBrush(Color.White),
                                     CSng(tt.pt.X * ratio), CSng(tt.pt.Y * ratio))
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("Error in trueData update: " + ex.Message)
            End Try
        End SyncLock

        Dim WorkingRes = settings.WorkingRes
        Dim cres = settings.captureRes
        Dim dres = settings.displayRes
        Dim resolutionDetails = "Input " + CStr(cres.Width) + "x" + CStr(cres.Height) + ", WorkingRes " + CStr(WorkingRes.Width) + "x" + CStr(WorkingRes.Height)
        camLabel(0).Text = "RGB"
        If picLabels(0) <> "" Then camLabel(0).Text = picLabels(0)
        If picLabels(1) <> "" Then camLabel(1).Text = picLabels(1)
        camLabel(2).Text = picLabels(2)
        camLabel(3).Text = picLabels(3)
        camLabel(0).Text += " - " + resolutionDetails
        If picLabels(1) = "" Or testAllRunning Then camLabel(1).Text = "Depth RGB"
        AlgorithmDesc.Text = textDesc
    End Sub
End Class