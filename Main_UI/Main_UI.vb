Imports System.Threading
Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports VB_Classes
Imports CS_Classes
Imports System.Management
Imports cvext = OpenCvSharp.Extensions
Imports System.ComponentModel
Imports System.Windows.Documents
#Region "Globals"
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


Public Class Main_UI
    Public Shared settings As jsonClass.ApplicationStorage
    Public Shared cameraNames As List(Of String)
    Dim jsonfs As New jsonClass.FileOperations
    Dim upArrow As Boolean, downArrow As Boolean

    Dim threadStartTime As DateTime

    Dim optionsForm As MainOptions
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
    Dim dst(camPic.Count - 1) As cvb.Mat

    Dim paintNewImages As Boolean
    Dim newCameraImages As Boolean

    Dim algorithmRefresh As Boolean
    Dim CodeLineCount As Integer
    Dim DrawingRectangle As Boolean
    Dim drawRect As New cvb.Rect
    Dim drawRectPic As Integer
    Dim externalPythonInvocation As Boolean
    Dim frameCount As Integer
    Dim GrabRectangleData As Boolean
    Public HomeDir As DirectoryInfo

    Dim LastX As Integer
    Dim LastY As Integer
    Dim mouseClickFlag As Boolean
    Dim ClickPoint As New cvb.Point
    Dim mousePicTag As Integer
    Dim mouseDownPoint As New cvb.Point
    Dim mouseMovePoint As New cvb.Point
    Dim mousePoint As New cvb.Point
    Dim activeMouseDown As Boolean

    Dim myBrush = New SolidBrush(Color.White)
    Dim groupList As New List(Of String)
    Dim TreeViewDialog As TreeviewForm
    Public fpsAlgorithm As Single
    Public fpsCamera As Single
    Dim picLabels() = {"", "", "", ""}
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen
    Dim textDesc As String = ""
    Dim textAdvice As String = ""
    Dim totalBytesOfMemoryUsed As Integer
    Dim trueData As New List(Of VB_Classes.TrueText)
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
    Dim pixelViewerRect As cvb.Rect
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
#Region "Non-volatile"
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
                    .captureRes = New cvb.Size(1920, 1080)
                    If .camera1920x1080Support(.cameraIndex) = False Then
                        .captureRes = New cvb.Size(1280, 720)
                        .WorkingRes = New cvb.Size(320, 180)
                    End If
                Case 180, 360, 720
                    .captureRes = New cvb.Size(1280, 720)
                Case 376, 188, 94
                    .captureRes = New cvb.Size(672, 376)
                Case 120, 240, 480
                    .captureRes = New cvb.Size(640, 480)
                    If .camera640x480Support(.cameraIndex) = False Then
                        .captureRes = New cvb.Size(1280, 720)
                        .WorkingRes = New cvb.Size(320, 180)
                    End If
            End Select

            Dim wh = .WorkingRes.Height
            ' desktop style is the default
            If .snap320 = False And .snap640 = False And .snapCustom = False Then .snap640 = True
            If .snap640 Then
                .locationMain.Item2 = 1321
                .locationMain.Item3 = 870
                If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 1096
                If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cvb.Size(640, 480) Else .displayRes = New cvb.Size(640, 360)
            ElseIf .snap320 Then
                .locationMain.Item2 = 683
                .locationMain.Item3 = 510
                If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 616
                If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cvb.Size(320, 240) Else .displayRes = New cvb.Size(320, 180)
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
                For i = 0 To MainOptions.resolutionList.Count - 1
                    If MainOptions.resolutionList(i).StartsWith(resStr) Then
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
            optionsForm = New MainOptions
            optionsForm.defineCameraResolutions(settings.cameraIndex)
        End With
    End Sub
    Public Sub jsonWrite()
        If TestAllButton.Text <> "Stop Test" Then ' don't save the algorithm name and group if testing all
            settings.algorithm = AvailableAlgorithms.Text
            settings.algorithmGroup = GroupName.Text
        End If

        settings.locationMain = New cvb.Vec4f(Me.Left, Me.Top, Me.Width, Me.Height)
        settings.treeButton = TreeButton.Checked
        settings.PixelViewerButton = False
        settings.displayRes = New cvb.Size(camPic(0).Width, camPic(0).Height) ' used only when .snapCustom is true
        If settings.translatorMode = "" Then settings.translatorMode = "VB.Net to C#"

        Dim setlist = New List(Of jsonClass.ApplicationStorage)
        setlist.Add(settings)
        jsonfs.Save(setlist)
    End Sub
    Private Sub OpenCVB_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyValue = Keys.Up Then upArrow = True
        If e.KeyValue = Keys.Down Then downArrow = True
    End Sub
    Private Sub updatePath(neededDirectory As String, notFoundMessage As String)
        Dim systemPath = Environment.GetEnvironmentVariable("Path")
        Dim foundDirectory As Boolean
        If Directory.Exists(neededDirectory) Then
            foundDirectory = True
            systemPath = neededDirectory + ";" + systemPath
        End If

        If foundDirectory = False And notFoundMessage.Length > 0 Then
            MsgBox(neededDirectory + " was not found.  " + notFoundMessage)
        End If
        Environment.SetEnvironmentVariable("Path", systemPath)
    End Sub
    Public Function validateRect(r As cvb.Rect, width As Integer, height As Integer) As cvb.Rect
        If r.Width < 0 Then r.Width = 1
        If r.Height < 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > width Then r.X = width
        If r.Y > height Then r.Y = height
        If r.X + r.Width > width Then r.Width = width - r.X
        If r.Y + r.Height > height Then r.Height = height - r.Y
        Return r
    End Function
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
                If infoLine.StartsWith("Options_") Then Continue While
                If infoLine.StartsWith("CPP_Basics") Then Continue While
                addNextAlgorithm(infoLine, lastNameSplit)
            End While
            sr.Close()
        Else
            AvailableAlgorithms.Enabled = False
            Dim keyIndex = GroupName.Items.IndexOf(GroupName.Text)
            Dim groupings = groupList(keyIndex)
            Dim split = Regex.Split(groupings, ",")
            AvailableAlgorithms.Items.Clear()
            For i = 1 To split.Length - 1
                If split(i).StartsWith("Options_") Then Continue For
                If split(i).StartsWith("CPP_Basics") Then Continue For
                addNextAlgorithm(split(i), lastNameSplit)
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
        Dim padY = 60
        If settings.snapCustom Then ' custom size - neither snap320 or snap640
            Dim ratio = settings.WorkingRes.Height / settings.WorkingRes.Width
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

        XYLoc.Location = New Point(camPic(2).Left, camPic(2).Top + camPic(2).Height)
    End Sub
    Private Sub BluePlusButton_Click(sender As Object, e As EventArgs) Handles BluePlusButton.Click
        Dim OKcancel = InsertAlgorithm.ShowDialog()
    End Sub
    Private Sub TreeButton_Click(sender As Object, e As EventArgs) Handles TreeButton.Click
        TreeButton.Checked = Not TreeButton.Checked
        settings.treeButton = TreeButton.Checked
        If TreeButton.Checked Then
            TreeViewDialog = New TreeviewForm
            TreeViewDialog.Show()
        Else
            If TreeViewDialog IsNot Nothing Then TreeViewDialog.Close()
        End If
    End Sub
    Private Sub TranslateButton_Click(sender As Object, e As EventArgs) Handles TranslateButton.Click
        Translator.Show()
    End Sub
    Private Sub AvailableAlgorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
        If AvailableAlgorithms.Text = "" Then
            Dim incr = 1
            If upArrow Then incr = -1
            upArrow = False
            downArrow = False
            AvailableAlgorithms.Text = AvailableAlgorithms.Items(AvailableAlgorithms.SelectedIndex + incr)
            Exit Sub
        End If
        If AvailableAlgorithms.Enabled Then
            If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e) ' if paused, then restart.
            jsonWrite()
            StartTask()
            updateAlgorithmHistory()
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

        trueData = New List(Of VB_Classes.TrueText)

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
    Private Sub ComplexityTimer_Tick(sender As Object, e As EventArgs) Handles ComplexityTimer.Tick
        While 1
            If Main_UI.settings.resolutionsSupported(settings.WorkingResIndex) Then
                setWorkingRes()
                Exit While
            Else
                settings.WorkingResIndex -= 1
                If settings.WorkingResIndex < 0 Then
                    settings.WorkingResIndex = Main_UI.settings.resolutionsSupported.Count - 1
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
        ClickPoint = New cvb.Point
        mousePoint = New cvb.Point

        StartTask()

        settings.WorkingResIndex -= 1
        If settings.WorkingResIndex < 0 Then
            settings.WorkingResIndex = Main_UI.settings.resolutionsSupported.Count - 1
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
    Private Sub setWorkingRes()
        Select Case settings.WorkingResIndex
            Case 0
                settings.WorkingRes = New cvb.Size(1920, 1080)
                settings.captureRes = New cvb.Size(1920, 1080)
            Case 1
                settings.WorkingRes = New cvb.Size(960, 540)
                settings.captureRes = New cvb.Size(1920, 1080)
            Case 2
                settings.WorkingRes = New cvb.Size(480, 270)
                settings.captureRes = New cvb.Size(1920, 1080)
            Case 3
                settings.WorkingRes = New cvb.Size(1280, 720)
                settings.captureRes = New cvb.Size(1280, 720)
            Case 4
                settings.WorkingRes = New cvb.Size(640, 360)
                settings.captureRes = New cvb.Size(1280, 720)
            Case 5
                settings.WorkingRes = New cvb.Size(320, 180)
                settings.captureRes = New cvb.Size(1280, 720)
            Case 6
                settings.WorkingRes = New cvb.Size(640, 480)
                settings.captureRes = New cvb.Size(640, 480)
            Case 7
                settings.WorkingRes = New cvb.Size(320, 240)
                settings.captureRes = New cvb.Size(640, 480)
            Case 8
                settings.WorkingRes = New cvb.Size(160, 120)
                settings.captureRes = New cvb.Size(640, 480)
            Case 9
                settings.WorkingRes = New cvb.Size(672, 376)
                settings.captureRes = New cvb.Size(672, 376)
            Case 10
                settings.WorkingRes = New cvb.Size(336, 188)
                settings.captureRes = New cvb.Size(672, 376)
            Case 11
                settings.WorkingRes = New cvb.Size(168, 94)
                settings.captureRes = New cvb.Size(672, 376)
        End Select
    End Sub

    Private Sub Advice_Click(sender As Object, e As EventArgs) Handles Advice.Click
        MsgBox(textAdvice)
    End Sub
    Private Sub jumpToAlgorithm(algName As String)
        If AvailableAlgorithms.Items.Contains(algName) = False Then
            AvailableAlgorithms.SelectedIndex = 0
        Else
            AvailableAlgorithms.SelectedItem = algName
        End If
    End Sub
    Private Sub algHistory_Clicked(sender As Object, e As EventArgs)
        arrowIndex = 0
        Dim item = TryCast(sender, ToolStripMenuItem)
        If AvailableAlgorithms.Items.Contains(item.Name) = False Then
            MsgBox("That algorithm was not found" + vbCrLf + vbCrLf + "The name may have changed or " + vbCrLf +
                   "The currently selected group does not contain " + item.Name + vbCrLf + "Change the group to <All> to guarantee access.")
        Else
            recentAlgorithm(item.Text)
        End If
    End Sub
    Private Sub BackButton_Click(sender As Object, e As EventArgs) Handles BackButton.Click
        If arrowIndex = 0 Then
            arrowList.Clear()
            For i = 0 To algHistory.Count - 1
                arrowList.Add(algHistory.ElementAt(i))
            Next
        End If
        arrowIndex = Math.Min(arrowList.Count - 1, arrowIndex + 1)
        jumpToAlgorithm(arrowList.ElementAt(arrowIndex))
    End Sub
    Private Sub ForwardButton_Click(sender As Object, e As EventArgs) Handles ForwardButton.Click
        If arrowIndex = 0 Then
            jumpToAlgorithm(AvailableAlgorithms.Items(Math.Min(AvailableAlgorithms.Items.Count - 1, AvailableAlgorithms.SelectedIndex + 1)))
        Else
            arrowIndex = Math.Max(0, arrowIndex - 1)
            jumpToAlgorithm(arrowList.ElementAt(arrowIndex))
        End If
    End Sub
    Private Sub recentAlgorithm(algName As String)
        AvailableAlgorithms.SelectedItem = algName
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
    Public Class compareAllowIdenticalString : Implements IComparer(Of String)
        Public Function Compare(ByVal a As String, ByVal b As String) As Integer Implements IComparer(Of String).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a >= b Then Return 1
            Return -1
        End Function
    End Class
    Private Function killPythonCameraOrTask() As Boolean
        Dim proc = Process.GetProcesses()
        Dim foundCamera As Boolean
        'Dim procList As New SortedList(Of String, String)(New compareAllowIdenticalString)
        For i = 0 To proc.Count - 1
            'procList.Add(proc(i).ProcessName, proc(i).ProcessName)
            If proc(i).ProcessName.ToLower.Contains("python") Then
                Try
                    If proc(i).HasExited = False Then
                        proc(i).Kill()
                        If proc(i).ProcessName.ToLower.Contains("pythonw") Then
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
    Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        jsonWrite()

        cameraTaskHandle = Nothing
        If TreeButton.Checked Then TreeViewDialog.Close()

        killPythonCameraOrTask()

        saveAlgorithmName = "" ' this will close the current algorithm.
    End Sub
    Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs) Handles RefreshTimer.Tick
        If AvailableAlgorithms.Items.Count = 0 Then Exit Sub
        If (paintNewImages Or algorithmRefresh) And AvailableAlgorithms.Text.StartsWith(saveAlgorithmName) Then
            camPic(0).Refresh()
            camPic(1).Refresh()
            camPic(2).Refresh()
            camPic(3).Refresh()
        End If
    End Sub
    Private Sub RecordWindowsVersion()
        Dim Version = Environment.OSVersion.Version
        Console.WriteLine("Windows version = " + CStr(Version.Major) + "." + CStr(Version.Minor) + " with build = " + CStr(Version.Build))
        If Version.Build >= 22000 Then windowsVersion = 11 Else windowsVersion = 10
    End Sub
    Private Sub PixelViewerButton_Click(sender As Object, e As EventArgs) Handles PixelViewerButton.Click
        If fpsTimer.Enabled Then
            SaveSetting("OpenCVB", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
            SaveSetting("OpenCVB", "PixelViewerTop", "PixelViewerTop", Me.Height)
            SaveSetting("OpenCVB", "PixelViewerWidth", "PixelViewerWidth", Me.Width)
        End If
        PixelViewerButton.Checked = Not PixelViewerButton.Checked
        settings.PixelViewerButton = PixelViewerButton.Checked
    End Sub
    Private Sub loadAlgorithmComboBoxes()
        ' we always need the number of lines from the AlgorithmList.txt file (and it is not always read when working with a subset of algorithms.)
        Dim AlgorithmListFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmList.txt")
        If AlgorithmListFileInfo.Exists = False Then
            MsgBox("The AlgorithmList.txt file is missing.  Run 'UI_Generator' or rebuild all to rebuild the user interface.")
            End
        End If
        Dim sr = New StreamReader(AlgorithmListFileInfo.FullName)
        Dim infoLine = sr.ReadLine
        Dim Split = Regex.Split(infoLine, "\W+")
        CodeLineCount = Split(1)
        While sr.EndOfStream = False
            infoLine = sr.ReadLine
            AlgorithmCount += 1
        End While
        sr.Close()

        Dim AlgorithmMapFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmGroupNames.txt")
        If AlgorithmMapFileInfo.Exists = False Then
            MsgBox("The AlgorithmGroupNames.txt file is missing.  Run 'UI_Generator' or rebuild all to rebuild the user interface.")
            End
        End If
        sr = New StreamReader(AlgorithmMapFileInfo.FullName)
        GroupName.Items.Clear()
        Dim lastNameSplit As String = "", lastSplit0 As String = ""
        While sr.EndOfStream = False
            infoLine = sr.ReadLine
            Split = Regex.Split(infoLine, ",")

            If Split(0).StartsWith("<") = False Then
                If Split(0).Contains("_") Then
                    If lastSplit0.Contains("_") = False Then
                        groupList.Add("")
                        GroupName.Items.Add("")
                    End If
                    Dim nameSplit = Split(0).Split("_")
                    If nameSplit(0) <> lastNameSplit And lastNameSplit <> "" Then
                        groupList.Add("")
                        GroupName.Items.Add("")
                    End If
                    lastNameSplit = nameSplit(0)
                    lastSplit0 = Split(0)
                ElseIf lastSplit0.Contains("_") Then
                    groupList.Add("")
                    GroupName.Items.Add("")
                    lastNameSplit = Split(0)
                End If
            End If
            groupList.Add(infoLine)
            GroupName.Items.Add(Split(0))
        End While
        sr.Close()
    End Sub
    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("The objective is to solve many small computer vision problems " + vbCrLf +
               "and do so in a way that enables any of the solutions to be reused." + vbCrLf +
               "The result is a toolkit for solving ever bigger and more difficult" + vbCrLf +
               "problems.  The hypothesis behind this approach is that human vision" + vbCrLf +
               "is not computationally intensive but is built on many almost trivial" + vbCrLf +
               "algorithms working together." + vbCrLf)
    End Sub
    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Me.Close()
    End Sub
    Private Sub ComplexityButton_Click(sender As Object, e As EventArgs) Handles ComplexityButton.Click
        If ComplexityTimer.Enabled = False Then
            Dim ret = MsgBox("Do you want to test the complexity of the current algorithm?" + vbCrLf +
                             "Algorithm will run at all available resolutions until you stop it.", MsgBoxStyle.OkCancel,
                         "Test algorithm at all resolutions.")
            If ret = MsgBoxResult.Ok Then
                complexityResults.Clear()
                ComplexityTimer.Interval = 30000
                complexityStartTime = Now
                ComplexityTimer.Enabled = True
                settings.WorkingResIndex = Main_UI.settings.resolutionsSupported.Count - 1 ' start smallest resolution
                ComplexityButton.Image = stopTest
                ComplexityTimer_Tick(sender, e)
            End If
        Else
            Dim sw = New StreamWriter(HomeDir.FullName + "Complexity/" + saveAlgorithmName + ".txt")
            For Each line In complexityResults
                sw.WriteLine(line)
            Next
            sw.Close()

            ComplexityButton.Image = complexityTest
            ComplexityTimer.Enabled = False
            complexityResults.Clear()
        End If
    End Sub
#End Region
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        threadStartTime = DateTime.Now

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture
        Dim args() = Environment.GetCommandLineArgs()

        HomeDir = If(args.Length > 1, New DirectoryInfo(CurDir() + "\..\..\..\..\"), New DirectoryInfo(CurDir() + "\..\..\..\..\..\"))
        jsonRead()

        ' currently the only commandline arg is the name of the algorithm to run.  Save it and continue...
        If args.Length > 1 Then
            Dim algorithm As String = "AddWeighted_PS.py"
            settings.algorithmGroup = "<All >"
            If args.Length > 2 Then ' arguments from python os.spawnv are passed as wide characters.  
                For i = 0 To args.Length - 1
                    algorithm += args(i)
                Next
            Else
                algorithm = args(1)
            End If
            Console.WriteLine("'" + algorithm + "' was provided in the command line arguments to OpenCVB")
            If algorithm = "Pyglet_Image_PS.py" Then End
            externalPythonInvocation = True ' we don't need to start python because it started OpenCVB.
        End If

        PausePlay = New Bitmap(HomeDir.FullName + "Main_UI/Data/PauseButton.png")
        stopTest = New Bitmap(HomeDir.FullName + "Main_UI/Data/StopTest.png")
        testAllToolbarBitmap = New Bitmap(HomeDir.FullName + "Main_UI/Data/testall.png")
        runPlay = New Bitmap(HomeDir.FullName + "Main_UI/Data/PauseButtonRun.png")

        setupAlgorithmHistory()

        Dim libraries = {"Cam_K4A.dll", "Cam_RS2.dll", "CPP_Code.dll", "Cam_MyntD.dll", "Cam_Zed2.dll", "Cam_ORB335L.dll"}
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

        Dim systemPath = Environment.GetEnvironmentVariable("Path")
        pythonPresent = InStr(systemPath.ToLower, "python")
        If pythonPresent = False Then
            MsgBox("Python needs to be in the path in order to run all the algorithms written in python." + vbCrLf +
                   "That is how you control which version of python is active for OpenCVB." + vbCrLf +
                   "All Python algorithms will be disabled for now...")
        End If

        ' Camera DLL's and OpenGL apps are built in Release mode even when configured for Debug (performance is much better).  
        ' OpenGL apps cannot be debugged from OpenCVB and the camera interfaces are not likely to need debugging.
        ' To debug a camera interface: change the Build Configuration and enable "Native Code Debugging" in the OpenCVB project.
        updatePath(HomeDir.FullName + "bin\Release\", "Release Version of camera DLL's.")
        updatePath(HomeDir.FullName + "bin\Debug\", "Debug Version of any camera DLL's.")

        Dim cudaPath = Environment.GetEnvironmentVariable("CUDA_PATH")
        If cudaPath IsNot Nothing Then
            updatePath(cudaPath, "Cuda - needed for StereoLabs")
            updatePath("C:\Program Files (x86)\ZED SDK\bin", "StereoLabs support")
        End If

        updatePath(HomeDir.FullName + "librealsense\build\Debug\", "Realsense camera support.")
        updatePath(HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Debug\", "Kinect camera support.")

        ' OpenCV needs to be in the path and the librealsense and K4A open source code needs to be in the path.
        updatePath(HomeDir.FullName + "librealsense\build\Release\", "Realsense camera support.")
        updatePath(HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Release\", "Kinect camera support.")

        updatePath(HomeDir.FullName + "OpenCV\Build\bin\Release\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
        updatePath(HomeDir.FullName + "OpenCV\Build\bin\Debug\", "OpenCV and OpenCV Contrib are needed for C++ classes.")

        updatePath(HomeDir.FullName + "OakD\build\depthai-core\Release\", "LibUsb for Luxonis")
        updatePath(HomeDir.FullName + "OakD\build\Release\", "Luxonis Oak-D camera support.")

        updatePath("C:\Program Files\OrbbecSDK 1.10.5\Example\bin", "OrbbecSDK.dll")

        ' the K4A depthEngine DLL is not included in the SDK.  It is distributed separately because it is NOT open source.
        ' The depthEngine DLL is supposed to be installed in C:\Program Files\Azure Kinect SDK v1.1.0\sdk\windows-desktop\amd64\$(Configuration)
        ' Post an issue if this Is Not a valid assumption
        Dim K4ADLL As New FileInfo("C:\Program Files\Azure Kinect SDK v1.4.1\sdk\windows-desktop\amd64\release\bin\depthengine_2_0.dll")
        If K4ADLL.Exists = False Then
            MsgBox("The Microsoft installer for the Kinect 4 Azure camera proprietary portion" + vbCrLf +
                   "was not installed in:" + vbCrLf + vbCrLf + K4ADLL.FullName + vbCrLf + vbCrLf +
                   "Did a new Version get installed?" + vbCrLf +
                   "Support for the K4A camera may not work until you update the code near this message.")
            Dim k4aIndex = cameraNames.IndexOf("Azure Kinect 4K")
            settings.cameraPresent(k4aIndex) = False ' we can't use this device
            settings.cameraSupported(k4aIndex) = False
        Else
            updatePath(K4ADLL.Directory.FullName, "Kinect depth engine dll.")
        End If

        If settings.cameraFound Then
            startCamera()
            While camera Is Nothing ' wait for camera to start...
            End While
        End If

        setupCamPics()

        If settings.treeButton Then TreeButton_Click(sender, e)

        loadAlgorithmComboBoxes()

        If settings.algorithmGroup.Contains("<All ") Then
            Dim searchStr = settings.algorithmGroup.Substring(0, InStr(settings.algorithmGroup, "("))
            For i = 0 To Math.Min(20, groupList.Count)
                If groupList(i).StartsWith(searchStr) Then
                    GroupName.SelectedItem() = groupList(i).Substring(0, InStr(groupList(i), ">") - 1) + ">"
                    Exit For
                End If
            Next
        Else
            GroupName.Text = settings.algorithmGroup
        End If

        If GroupName.SelectedItem() Is Nothing Then GroupName.SelectedItem() = groupList(1)

        If AvailableAlgorithms.Items.Count = 0 Then
            MsgBox("There were no algorithms listed for the " + GroupName.Text + vbCrLf +
                   "This usually indicates something has changed with " + vbCrLf + "UIGenerator")
        Else
            If settings.algorithm Is Nothing Then
                AvailableAlgorithms.SelectedIndex = 0
                settings.algorithm = AvailableAlgorithms.Text
            End If
            If AvailableAlgorithms.Items.Contains(settings.algorithm) Then
                AvailableAlgorithms.Text = settings.algorithm
            Else
                AvailableAlgorithms.SelectedIndex = 0
            End If
            jsonWrite()
        End If

        AvailableAlgorithms.Width = 600
        AvailableAlgorithms.ComboBox.Select()
        AlgorithmDesc.Top = ToolStrip1.Top
        AlgorithmDesc.Left = ToolStrip1.Left + 830
        AlgorithmDesc.Width = ToolStrip1.Left + ToolStrip1.Width - AlgorithmDesc.Left
        AlgorithmDesc.Height = ToolStrip1.Height

        RecordWindowsVersion()
        fpsTimer.Enabled = True
        XYLoc.Text = "(x:0, y:0) - last click point at: (x:0, y:0)"
    End Sub

    Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
        Static lastAlgorithmFrame As Integer
        Static lastCameraFrame As Integer

        If camera Is Nothing Then Exit Sub
        If lastAlgorithmFrame > frameCount Then lastAlgorithmFrame = 0
        If lastCameraFrame > camera.cameraFrameCount Then lastCameraFrame = 0
        If TreeViewDialog IsNot Nothing Then
            If TreeViewDialog.TreeView1.IsDisposed Then TreeButton.CheckState = CheckState.Unchecked
        End If

        Static fpsListA As New List(Of Single)
        Static fpsListC As New List(Of Single)
        If pauseAlgorithmThread = False Then
            Dim countFrames = frameCount - lastAlgorithmFrame
            lastAlgorithmFrame = frameCount
            fpsListA.Add(CSng(countFrames / (fpsTimer.Interval / 1000)))

            Dim camFrames = camera.cameraFrameCount - lastCameraFrame
            lastCameraFrame = camera.cameraFrameCount
            fpsListC.Add(CSng(camFrames / (fpsTimer.Interval / 1000)))
            If cameraTaskHandle Is Nothing Then Exit Sub
            Dim cameraName = settings.cameraName
            cameraName = cameraName.Replace(" 2/2i", "")
            cameraName = cameraName.Replace(" camera", "")
            cameraName = cameraName.Replace(" Camera", "")
            cameraName = cameraName.Replace("Intel(R) RealSense(TM) Depth ", "Intel D")

            fpsAlgorithm = fpsListA.Average
            fpsCamera = fpsListC.Average
            If fpsAlgorithm >= 100 Then fpsAlgorithm = 99
            If fpsCamera >= 100 Then fpsCamera = 99
            Me.Text = "OpenCVB - " + Format(CodeLineCount, "###,##0") + " lines / " + CStr(AlgorithmCount) + " algorithms = " +
                      CStr(CInt(CodeLineCount / AlgorithmCount)) + " lines each (avg) - " + cameraName +
                      " - Camera FPS/task FPS: " + Format(fpsAlgorithm, "0") + "/" + Format(fpsCamera, "0")
            If fpsListA.Count > 5 Then
                fpsListA.RemoveAt(0)
                fpsListC.RemoveAt(0)
            End If
        End If
    End Sub
    Private Sub Options_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
        If TestAllTimer.Enabled Then TestAllButton_Click(sender, e)
        Dim saveCameraIndex = settings.cameraIndex

        optionsForm.MainOptions_Load(sender, e)
        optionsForm.cameraRadioButton(settings.cameraIndex).Checked = True
        Dim resStr = CStr(settings.WorkingRes.Width) + "x" + CStr(settings.WorkingRes.Height)
        For i = 0 To MainOptions.resolutionList.Count - 1
            If MainOptions.resolutionList(i).StartsWith(resStr) Then
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
                        MainOptions.defineCameraResolutions(settings.cameraIndex)
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
            ClickPoint = New cvb.Point
            mousePoint = New cvb.Point
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
        parms.mainFormLocation = New cvb.Rect(Me.Left, Me.Top, Me.Width, Me.Height)

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
            Dim rect = New cvb.Rect(CInt(r.X * ratio), CInt(r.Y * ratio),
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
                            Dim camSize = New cvb.Size(camPic(0).Size.Width, camPic(0).Size.Height)
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
        If camLabel(0).Text <> "RGB - " + resolutionDetails Then
            camLabel(0).Text = "RGB"
            If picLabels(0) <> "" Then camLabel(0).Text = picLabels(0)
            If picLabels(1) <> "" Then camLabel(1).Text = picLabels(1)
            camLabel(0).Text += " - " + resolutionDetails
        End If
        camLabel(2).Text = picLabels(2)
        camLabel(3).Text = picLabels(3)
        If picLabels(1) = "" Or testAllRunning Then camLabel(1).Text = "Depth RGB"
        AlgorithmDesc.Text = textDesc
    End Sub
    Public Sub setupNewCPPalgorithm(algorithmName As String)
        Dim functionNames = New FileInfo(HomeDir.FullName + "CPP_Code\CPP_Enum.h")
        Dim lines = File.ReadAllLines(functionNames.FullName)

        ' is it already up to date in the CPP_Enum.h file?
        For Each line In lines
            If line.Contains("_" + algorithmName) Then Exit Sub
        Next

        Dim sw = New StreamWriter(functionNames.FullName, False)
        For Each line In lines
            If line.Contains("_Stable_BasicsCount_CC,") Then
                sw.WriteLine(vbTab + "_" + algorithmName + ",")
            End If
            sw.WriteLine(line)
        Next
        sw.Close()

        Dim externNames = New FileInfo(HomeDir.FullName + "CPP_Code\CPP_Externs.h")
        lines = File.ReadAllLines(externNames.FullName)

        sw = New StreamWriter(externNames.FullName, False)
        For Each line In lines
            If line.Contains("// end of switch - don't remove...") Then
                sw.WriteLine(vbTab + vbTab + "case _" + algorithmName + ":")
                sw.WriteLine(vbTab + vbTab + "{ task->alg = new " + algorithmName + "(); task->alg->traceName = " +
                               """" + algorithmName + """; break; }")
            End If
            sw.WriteLine(line)
        Next
        sw.Close()
    End Sub
    Private Sub startCamera()
        paintNewImages = False
        newCameraImages = False
        If cameraTaskHandle Is Nothing Then
            cameraTaskHandle = New Thread(AddressOf CameraTask)
            cameraTaskHandle.Name = "Camera Task"
            cameraTaskHandle.Start()
        End If
    End Sub
    Private Function getCamera() As Object
        Select Case settings.cameraName
            Case "Azure Kinect 4K"
                Return New CameraKinect(settings.WorkingRes, settings.captureRes, settings.cameraName)
            Case "Intel(R) RealSense(TM) Depth Camera 435i"
                Return New CameraRS2(settings.WorkingRes, settings.captureRes, "Intel RealSense D435I")
            Case "Intel(R) RealSense(TM) Depth Camera 455"
                Return New CameraRS2(settings.WorkingRes, settings.captureRes, "Intel RealSense D455")
            Case "Oak-D camera"
                Return Nothing ' special handling required.  See CameraTask...
            Case "StereoLabs ZED 2/2i"
                Return New CameraZED2(settings.WorkingRes, settings.captureRes, settings.cameraName)
            Case "MYNT-EYE-D1000"
                Return New CameraMyntD(settings.WorkingRes, settings.captureRes, settings.cameraName)
            Case "Orbbec Gemini 335L"
                Return New CameraORB(settings.WorkingRes, settings.captureRes, settings.cameraName)
        End Select
        Return New CameraKinect(settings.WorkingRes, settings.captureRes, settings.cameraName)
    End Function
    Private Sub CameraTask()
        restartCameraRequest = True
        Static saveWorkingRes As cvb.Size
        For i = 0 To mbuf.Count - 1
            mbuf(i).color = New cvb.Mat(settings.WorkingRes, cvb.MatType.CV_8UC3)
            mbuf(i).leftView = New cvb.Mat(settings.WorkingRes, cvb.MatType.CV_8UC3)
            mbuf(i).rightView = New cvb.Mat(settings.WorkingRes, cvb.MatType.CV_8UC3)
            mbuf(i).pointCloud = New cvb.Mat(settings.WorkingRes, cvb.MatType.CV_32FC3)
        Next

        While 1
            If restartCameraRequest Or settings.WorkingRes <> saveWorkingRes Then
                saveWorkingRes = settings.WorkingRes
                If settings.cameraIndex = 3 Then
                    ' special handling for the Oak-D camera as it cannot be restarted.
                    ' It is my problem but I don't see how to fix it.
                    ' The Oak-D interface cannot run any resolution other than 1280x720 in OpenCVB.
                    ' Changing the working res is not a problem so just leave it open.
                    ' Oak-D camera cannot be restarted without restarting OpenCVB.
                    ' Leave it alone once it is started...
                    settings.captureRes = New cvb.Size(1280, 720)
                    If camera Is Nothing Then camera = New CameraOakD(settings.WorkingRes, settings.captureRes, settings.cameraName)
                Else
                    If camera IsNot Nothing Then camera.stopCamera()
                    camera = getCamera()
                    newCameraImages = False
                End If
            End If
            If camera Is Nothing Then Continue While ' transition from one camera to another.  Problem showed up once.
            If restartCameraRequest = False Then camera.GetNextFrame(settings.WorkingRes)

            ' The first few frames from the camera are junk.  Skip them.
            SyncLock cameraLock
                mbuf(mbIndex) = camera.mbuf(camera.mbIndex)
                camera.mbindex += 1
                If camera.mbindex >= mbuf.Count Then camera.mbindex = 0

                Try
                    If camera.mbuf(mbIndex).color.width > 0 Then
                        paintNewImages = True ' trigger the paint 
                        newCameraImages = True
                    End If
                Catch ex As Exception
                    Console.WriteLine(ex.Message + " in CameraTask - very unusual but recoverable.  Switching buffers.")
                End Try
            End SyncLock
            If cameraTaskHandle Is Nothing Then
                camera.stopCamera()
                Exit Sub
            End If

            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
            totalBytesOfMemoryUsed = currentProcess.WorkingSet64 / (1024 * 1024)
            restartCameraRequest = False
        End While
    End Sub
    Private Sub AlgorithmTask(ByVal parms As VB_Classes.VBtask.algParms)
        If parms.algName = "" Then Exit Sub
        algorithmQueueCount += 1
        ' the duration of any algorithm varies a lot so wait here if previous algorithm is not finished.
        SyncLock algorithmThreadLock
            algorithmQueueCount -= 1
            AlgorithmTestAllCount += 1
            drawRect = New cvb.Rect
            Dim task = New VBtask(parms)
            Static algolist As algorithmList = New algorithmList

            task.algorithmObject = algolist.createAlgorithm(parms.algName)
            textDesc = task.algorithmObject.desc

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
            If task.drawRect <> New cvb.Rect Then
                ' relative size of algorithm size image to displayed image
                Dim ratio = camPic(0).Width / task.WorkingRes.Width
                drawRect = New cvb.Rect(task.drawRect.X * ratio, task.drawRect.Y * ratio,
                                       task.drawRect.Width * ratio, task.drawRect.Height * ratio)
            End If

            Dim saveWorkingRes = settings.WorkingRes
            task.labels = {"", "", "", ""}
            mousePoint = New cvb.Point(task.WorkingRes.Width / 2, task.WorkingRes.Height / 2) ' mouse click point default = center of the image

            Dim saveDrawRect As cvb.Rect
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
                            Dim tmpDrawRect = New cvb.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                            task.drawRect = New cvb.Rect
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
                        If task.mouseMovePoint = New cvb.Point(0, 0) Then
                            task.mouseMovePoint = New cvb.Point(task.WorkingRes.Width / 2, task.WorkingRes.Height / 2)
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
                'If parms.algName.EndsWith("_CS") Then
                '    Static findCSharp = New CS_Classes.CSAlgorithmList()

                '    If task.csAlgorithmObject Is Nothing Then
                '        task.csAlgorithmObject = findCSharp.createCSAlgorithm(parms.algName, task)
                '        task.desc = task.csAlgorithmObject.desc
                '    End If
                'End If

                'If parms.algName.EndsWith("_CPP") Then
                '    Static findCPP = New CPP_AlgorithmList()
                '    If task.cppAlgorithmObject Is Nothing Then
                '        task.cppAlgorithmObject = findCPP.createCPPAlgorithm(parms.algName)
                '        'task.desc = task.cppAlgorithmObject.desc
                '    End If
                'End If

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
                    drawRect = New cvb.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                End If
                If task.drawRectClear Then
                    drawRect = New cvb.Rect
                    task.drawRect = drawRect
                    task.drawRectClear = False
                End If

                pixelViewerRect = task.pixelViewerRect
                pixelViewTag = task.pixelViewTag

                task.fpsRate = If(fpsAlgorithm = 0, 1, fpsAlgorithm)

                If task.paused = False Then
                    SyncLock trueData
                        If task.trueData.Count Then
                            trueData = New List(Of VB_Classes.TrueText)(task.trueData)
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
End Class
