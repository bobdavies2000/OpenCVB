﻿Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Threading
Imports cv = OpenCvSharp
Imports VBClasses
Imports System.Management
Imports cvext = OpenCvSharp.Extensions
#Region "Globals and stable code"
Module OpenCVB_module
    ' Public bufferLock As New Mutex(True, "bufferLock") ' this is a global lock on the camera buffers.
    Public mouseLock As New Mutex(True, "mouseLock") ' global lock for use with mouse clicks.
    Public algorithmThreadLock As New Mutex(True, "AlgorithmThreadLock")
    Public cameraLock As New Mutex(True, "cameraLock")
    Public trueTextLock As New Mutex(True, "trueTextLock")
    Public Declare Function GetWindowRect Lib "user32" (ByVal HWND As Integer, ByRef lpRect As RECT) As Integer
    <StructLayout(LayoutKind.Sequential)> Public Structure RECT
        Dim Left As Integer
        Dim Top As Integer
        Dim Right As Integer
        Dim Bottom As Integer
    End Structure
End Module
#End Region

Public Class Main
    Dim trueData As New List(Of TrueText)
    Dim algolist As algorithmList = New algorithmList
    Public Shared settings As jsonClass.ApplicationStorage
    Dim saveworkRes As cv.Size
    Dim saveCameraName As String
    Dim pauseCameraTask As Boolean

    Dim jsonfs As New jsonClass.FileOperations
    Dim optionsForm As Options
    Dim groupList As New List(Of String)

    Public HomeDir As DirectoryInfo
    Public groupButtonSelection As String
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
    Public DevicesChanged As Boolean
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

    Dim LastX As Integer
    Dim LastY As Integer
    Dim mouseClickFlag As Boolean
    Dim activateTaskForms As Boolean
    Dim ClickPoint As New cv.Point ' last place where mouse was clicked.
    Dim mousePicTag As Integer
    Dim mouseDownPoint As New cv.Point
    Dim mouseMovePoint As New cv.Point ' last place the mouse was located in any of the OpenCVB images.
    Dim mouseDisplayPoint As New cv.Point
    Dim activeMouseDown As Boolean

    Dim myBrush = New SolidBrush(Color.White)
    Public algorithmFPSrate As Single
    Dim fpsListA As New List(Of Single)
    Dim fpsListC As New List(Of Single)
    Public fpsCamera As Single
    Dim picLabels() = {"", "", "", ""}
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen
    Dim textDesc As String = ""
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

    Public testAllRunning As Boolean

    Public colorTransitionCount As Integer
    Public colorScheme As String
    Dim pythonPresent As Boolean

    Dim testAllResolutionCount As Integer
    Dim testAllStartingRes As Integer
    Dim testAllEndingRes As Integer
    Dim windowsVersion As Integer
    Dim magIndex As Integer
    Dim motionLabel As String

    Dim depthAndCorrelationText As String
    Public Shared cameraNames As New List(Of String)({"StereoLabs ZED 2/2i",
                                                      "Orbbec Gemini 335",
                                                      "Orbbec Gemini 336L",
                                                      "Oak-D camera",
                                                      "Intel(R) RealSense(TM) Depth Camera 435i",
                                                      "Intel(R) RealSense(TM) Depth Camera 455"})
    Public Sub jsonRead()
        jsonfs.jsonFileName = HomeDir.FullName + "settings.json"
        settings = jsonfs.Load()(0)
        ' The camera names are defined in VBtask.algParms.cameraNames
        ' the 3 lists below must have an entry for each camera - supported/640x480/1920...
        '  cameraNames = New List(Of String)(VB_Classes.VBtask.algParms.cameraNames)  ' <<<<<<<<<<<< here is the list of supported cameras.
        With settings
            .cameraSupported = New List(Of Boolean)({True, True, True, True, True, False, True, True})
            .camera640x480Support = New List(Of Boolean)({False, True, True, False, False, False, True, True})
            .camera1920x1080Support = New List(Of Boolean)({True, False, False, False, True, False, False, False})
            Dim defines = New FileInfo(HomeDir.FullName + "Cameras\CameraDefines.hpp")
            Dim stereoLabsDefineIsOff As Boolean
            Dim sr = New StreamReader(defines.FullName)
            Dim zedIndex = cameraNames.IndexOf("StereoLabs ZED 2/2i")
            While sr.EndOfStream = False
                Dim nextLine = sr.ReadLine
                If nextLine.Contains("STEREOLAB") Then
                    If nextLine.StartsWith("//") Then
                        .cameraSupported(zedIndex) = False
                        stereoLabsDefineIsOff = True
                    End If
                End If
            End While

            sr.Close()

            ' checking the list for specific missing device here...
            Dim usbList = USBenumeration()
            Dim testlist As New List(Of String)
            For Each usbDevice In usbList
                If LCase(usbDevice).Contains("orb") Then testlist.Add(usbDevice) ' debugging assistance...
            Next

            .cameraPresent = New List(Of Boolean)
            For i = 0 To cameraNames.Count - 1
                Dim searchname = cameraNames(i)
                Dim present As Boolean = False
                If searchname.Contains("Oak-D") Then searchname = "Movidius MyriadX"
                If stereoLabsDefineIsOff = False Then
                    If searchname.StartsWith("StereoLabs ZED 2/2i") Then searchname = "ZED 2"
                End If

                Dim subsetList As New List(Of String)
                For Each usbDevice In usbList
                    If usbDevice.Contains("Orb") Then subsetList.Add(usbDevice)
                    If usbDevice.Contains(searchname) Then present = True
                Next
                .cameraPresent.Add(present <> False)
            Next

            For i = 0 To cameraNames.Count - 1
                If cameraNames(i).StartsWith("Orbbec") Then
                    If cameraNames(i) = .cameraName Then
                        .cameraIndex = i
                        Exit For
                    End If
                Else
                    If cameraNames(i).Contains(.cameraName) And .cameraName <> "" Then
                        .cameraIndex = i
                        Exit For
                    End If
                End If
            Next

            If .cameraName = "" Or .cameraPresent(.cameraIndex) = False Then
                For i = 0 To cameraNames.Count - 1
                    If .cameraPresent(i) Then
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

            If .cameraPresent(zedIndex) And .cameraSupported(zedIndex) = False And stereoLabsDefineIsOff = False Then
                MessageBox.Show("A StereoLabls ZED 2 camera is present but OpenCVB's" + vbCrLf +
                       "Cam_Zed2.dll has not been built with the SDK." + vbCrLf + vbCrLf +
                       "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                       "and rerun Update_All.bat to get the StereoLabs SDK.")
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
                MessageBox.Show("There are no supported cameras present!" + vbCrLf + vbCrLf)
            End If

            If settings.testAllDuration < 5 Then settings.testAllDuration = 5
            If settings.fontInfo Is Nothing Then settings.fontInfo = New Font("Tahoma", 9)

            If .workRes = New cv.Size Then .workRes = New cv.Size(640, 480)
            Select Case .workRes.Height
                Case 270, 540, 1080
                    .captureRes = New cv.Size(1920, 1080)
                    If .camera1920x1080Support(.cameraIndex) = False Then
                        .captureRes = New cv.Size(1280, 720)
                        .workRes = New cv.Size(320, 180)
                    End If
                Case 180, 360, 720
                    .captureRes = New cv.Size(1280, 720)
                Case 376, 188, 94
                    If settings.cameraName <> "StereoLabs ZED 2/2i" Then
                        MessageBox.Show("The json settings don't appear to be correct!" + vbCrLf +
                                "The 'settings.json' file will be removed" + vbCrLf +
                                "and rebuilt with default settings upon restart.")
                        Dim fileinfo As New FileInfo(jsonfs.jsonFileName)
                        fileinfo.Delete()
                        End
                    End If
                    .captureRes = New cv.Size(672, 376)
                Case 120, 240, 480
                    .captureRes = New cv.Size(640, 480)
                    If .camera640x480Support(.cameraIndex) = False Then
                        .captureRes = New cv.Size(1280, 720)
                        .workRes = New cv.Size(320, 180)
                    End If
            End Select

            Dim wh = .workRes.Height
            ' desktop style is the default
            If .snap320 = False And .snap640 = False And .snapCustom = False Then .snap640 = True
            If .snap640 Then
                .locationMain.Item2 = 1321
                .locationMain.Item3 = 845
                If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 1096
                If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cv.Size(640, 480) Else .displayRes = New cv.Size(640, 360)
            ElseIf .snap320 Then
                .locationMain.Item2 = 683
                .locationMain.Item3 = 510
                If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 616
                If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cv.Size(320, 240) Else .displayRes = New cv.Size(320, 180)
            End If

            Dim border As Integer = 6
            Dim defaultWidth = .workRes.Width * 2 + border * 7
            Dim defaultHeight = .workRes.Height * 2 + ToolStrip1.Height + border * 12
            If Me.Height < 50 Then
                Me.Width = defaultWidth
                Me.Height = defaultHeight
            End If

            If .fontInfo Is Nothing Then .fontInfo = New Font("Tahoma", 9)
            If settings.groupComboText = "" Then settings.groupComboText = "< All >"

            If testAllRunning = False Then
                Dim resStr = CStr(.workRes.Width) + "x" + CStr(.workRes.Height)
                For i = 0 To Options.resolutionList.Count - 1
                    If Options.resolutionList(i).StartsWith(resStr) Then
                        .workResIndex = i
                        Exit For
                    End If
                Next
            End If

            .desiredFPS = 60
            Me.Left = .locationMain.Item0
            Me.Top = .locationMain.Item1
            Me.Width = .locationMain.Item2
            Me.Height = .locationMain.Item3

            optionsForm = New Options
            optionsForm.defineCameraResolutions(settings.cameraIndex)
        End With
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
    Public Function USBenumeration() As List(Of String)
        Static usblist As New List(Of String)
        Dim info As ManagementObject
        Dim search As ManagementObjectSearcher
        search = New ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
        If usblist.Count = 0 Then
            For Each info In search.Get()
                Dim Name = CType(info("Caption"), String)
                If Name IsNot Nothing Then
                    usblist.Add(Name)
                    ' This enumeration can tell us about the cameras present.  Built on first pass.
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
                        Debug.WriteLine(Name) ' looking for new cameras 
                    End If
                End If
            Next
        End If
        Return usblist
    End Function
    Private Function getCamera() As Object
        Select Case settings.cameraName
            Case "Intel(R) RealSense(TM) Depth Camera 455", "Intel(R) RealSense(TM) Depth Camera 435i"
                Return New CameraRS2(settings.workRes, settings.captureRes, settings.cameraName)
                'Case "Oak-D camera"
                '    Return New CameraOakD_CPP(settings.workRes, settings.captureRes, settings.cameraName)
            Case "StereoLabs ZED 2/2i"
                Return New CameraZed2(settings.workRes, settings.captureRes, settings.cameraName)
            Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                Return New CameraORB(settings.workRes, settings.captureRes, settings.cameraName)
                'Return New CameraORB_CPP(settings.workRes, settings.captureRes, settings.cameraName)
        End Select
        Return New CameraRS2(settings.workRes, settings.captureRes, settings.cameraName)
    End Function
    Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        jsonWrite()
        cameraShutdown = True
        Application.DoEvents()
        End
    End Sub
    Private Sub CameraTask()
        uiColor = New cv.Mat(settings.workRes, cv.MatType.CV_8UC3)
        uiLeft = New cv.Mat(settings.workRes, cv.MatType.CV_8UC3)
        uiRight = New cv.Mat(settings.workRes, cv.MatType.CV_8UC3)
        uiPointCloud = New cv.Mat(settings.workRes, cv.MatType.CV_32FC3)
        While 1
            If settings.workRes <> saveworkRes Or saveCameraName <> settings.cameraName Then
                If saveCameraName = settings.cameraName And camera IsNot Nothing Then camera.stopCamera()
                saveworkRes = settings.workRes
                saveCameraName = settings.cameraName
                camera = getCamera()
                newCameraImages = False
            ElseIf pauseCameraTask = False Then
                camera.GetNextFrame(settings.workRes)

                ' The first few frames from the camera are junk.  Skip them.
                SyncLock cameraLock
                    If camera.uicolor IsNot Nothing Then
                        uiColor = camera.uiColor.clone
                        uiLeft = camera.uiLeft.clone
                        uiRight = camera.uiRight.clone
                        ' a problem with the K4A interface was corrected here...
                        If camera.uipointcloud Is Nothing Then
                            camera.uipointcloud = New cv.Mat(settings.workRes, cv.MatType.CV_32FC3)
                        End If
                        uiPointCloud = camera.uiPointCloud.clone

                        newCameraImages = True ' trigger the algorithm task
                    End If
                End SyncLock

            End If
            If cameraShutdown Then
                camera.stopCamera()
                Exit Sub
            End If
        End While
    End Sub
    Private Sub OptionsButton_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
        If TestAllTimer.Enabled Then TestAllButton_Click(sender, e)
        Dim saveCameraIndex = settings.cameraIndex

        optionsForm.MainOptions_Load(sender, e)
        optionsForm.cameraRadioButton(settings.cameraIndex).Checked = True
        Dim resStr = CStr(settings.workRes.Width) + "x" + CStr(settings.workRes.Height)
        For i = 0 To Options.resolutionList.Count - 1
            If Options.resolutionList(i).StartsWith(resStr) Then
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

            jsonWrite()
            jsonRead() ' this will apply all the changes...
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
            mouseDisplayPoint.X = e.X
            mouseDisplayPoint.Y = e.Y
            mouseDisplayPoint *= settings.workRes.Width / camPic(0).Width

            XYLoc.Text = mouseDisplayPoint.ToString + ", last click point at: " + ClickPoint.ToString

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

        SyncLock cameraLock
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
                        For i = 0 To dst.Count - 1
                            If dst(i).Width > 0 Then
                                Dim tmp = dst(i).Resize(camSize)
                                cvext.BitmapConverter.ToBitmap(tmp, camPic(i).Image)
                            End If
                        Next
                    End If
                End If
            End If
        End SyncLock

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
        Dim resolutionDetails = "Input " + CStr(cres.Width) + "x" + CStr(cres.Height) + ", workRes " + CStr(workRes.Width) + "x" + CStr(workRes.Height)
        resolutionDetails += " - Motion: " + motionLabel
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
        magIndex += 1
    End Sub
    Private Sub MagnifyTimer_Tick(sender As Object, e As EventArgs) Handles MagnifyTimer.Tick
        Dim ratio = task.dst2.Width / camPic(0).Width
        Dim r = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
        r = validateRect(r, dst(drawRectPic).Width, dst(drawRectPic).Height)
        If r.Width = 0 Or r.Height = 0 Then Exit Sub
        Dim img = dst(drawRectPic)(r).Resize(New cv.Size(drawRect.Width * 5, drawRect.Height * 5))
        cv.Cv2.ImShow("DrawRect Region " + CStr(magIndex), img)
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
        If textDesc <> "" Then
            AlgDescription.Text = textDesc
            textDesc = ""
        End If
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

            algorithmFPSrate = fpsListA.Average
            fpsCamera = CInt(fpsListC.Average)
            If algorithmFPSrate >= 100 Then algorithmFPSrate = 99
            If fpsCamera >= 100 Then fpsCamera = 99
            Me.Text = "OpenCVB - " + Format(CodeLineCount, "###,##0") + " lines / " +
                          CStr(algorithmCount) + " algorithms = " +
                          CStr(CInt(CodeLineCount / algorithmCount)) + " lines each (avg) - " +
                          cameraName + " - Camera FPS/task FPS: " + Format(fpsCamera, "0") + "/" +
                          Format(algorithmFPSrate, "0")
            If fpsListA.Count > 5 Then
                fpsListA.RemoveAt(0)
                fpsListC.RemoveAt(0)
            End If
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
        End If
        If AvailableAlgorithms.Items(AvailableAlgorithms.SelectedIndex + 1) = "" Then
            AvailableAlgorithms.SelectedIndex += 2
        Else
            AvailableAlgorithms.SelectedIndex += 1
        End If

        ' skip testing the OpenGL algorithms.  They are only for visualizations - not for other algorithms to use.
        For i = AvailableAlgorithms.SelectedIndex To AvailableAlgorithms.Items.Count - 1
            If AvailableAlgorithms.Text.StartsWith("OpenGL_") = False Then Exit For
            AvailableAlgorithms.SelectedIndex += 1
        Next

        ' skip testing the Fractal_ algorithms.  They are only for visualizations - not for other algorithms to use.
        For i = AvailableAlgorithms.SelectedIndex To AvailableAlgorithms.Items.Count - 1
            If AvailableAlgorithms.Text.StartsWith("Fractal_") = False Then Exit For
            AvailableAlgorithms.SelectedIndex += 1
        Next

        ' skip testing the Benfor_ algorithms.  They are only for visualizations - not for other algorithms to use.
        For i = AvailableAlgorithms.SelectedIndex To AvailableAlgorithms.Items.Count - 1
            If AvailableAlgorithms.Text.StartsWith("Benford_") = False Then Exit For
            AvailableAlgorithms.SelectedIndex += 1
        Next

        ' skip testing the GIF_ algorithms.  They are only for visualizations - not for other algorithms to use.
        For i = AvailableAlgorithms.SelectedIndex To AvailableAlgorithms.Items.Count - 1
            If AvailableAlgorithms.Text.StartsWith("GIF_") = False Then Exit For
            AvailableAlgorithms.SelectedIndex += 1
        Next

        ' skip testing the Bitmap_ algorithms.  They are only for visualizations - not for other algorithms to use.
        For i = AvailableAlgorithms.SelectedIndex To AvailableAlgorithms.Items.Count - 1
            If AvailableAlgorithms.Text.StartsWith("Bitmap_") = False Then Exit For
            AvailableAlgorithms.SelectedIndex += 1
        Next

        ' skip testing the XO_ algorithms.  They are obsolete.
        If AvailableAlgorithms.Text.StartsWith("XO_") Then AvailableAlgorithms.SelectedIndex = 0

        TestAllTimer.Interval = settings.testAllDuration * 1000
        Static startingAlgorithm = AvailableAlgorithms.Text
        If AvailableAlgorithms.Text = startingAlgorithm And AlgorithmTestAllCount > 1 Then
            If settings.workResIndex > testAllEndingRes Then
                While 1
                    settings.cameraIndex += 1
                    If settings.cameraIndex >= cameraNames.Count - 1 Then settings.cameraIndex = 0
                    settings.cameraName = cameraNames(settings.cameraIndex)
                    If settings.cameraPresent(settings.cameraIndex) Then
                        Options.defineCameraResolutions(settings.cameraIndex)
                        setupTestAll()
                        settings.workResIndex = testAllStartingRes
                        Exit While
                    End If
                End While
                ' extra time for the camera to restart...
                TestAllTimer.Interval = settings.testAllDuration * 1000 * 3
            End If

            setworkRes()

            jsonWrite()
            jsonRead()
            LineUpCamPics()

            ' when switching resolution, best to reset these as the move from higher to lower res
            ' could mean the point is no longer valid.
            ClickPoint = New cv.Point
            mouseDisplayPoint = New cv.Point
        End If

        Static saveLastAlgorithm = AvailableAlgorithms.Text
        If saveLastAlgorithm <> AvailableAlgorithms.Text Then
            settings.workResIndex += 1
            saveLastAlgorithm = AvailableAlgorithms.Text
        End If
        StartAlgorithm()
    End Sub
    Private Sub setworkRes()
        Select Case settings.workResIndex
            Case 0
                settings.workRes = New cv.Size(1920, 1080)
                settings.captureRes = New cv.Size(1920, 1080)
            Case 1
                settings.workRes = New cv.Size(960, 540)
                settings.captureRes = New cv.Size(1920, 1080)
            Case 2
                settings.workRes = New cv.Size(480, 270)
                settings.captureRes = New cv.Size(1920, 1080)
            Case 3
                settings.workRes = New cv.Size(1280, 720)
                settings.captureRes = New cv.Size(1280, 720)
            Case 4
                settings.workRes = New cv.Size(640, 360)
                settings.captureRes = New cv.Size(1280, 720)
            Case 5
                settings.workRes = New cv.Size(320, 180)
                settings.captureRes = New cv.Size(1280, 720)
            Case 6
                settings.workRes = New cv.Size(640, 480)
                settings.captureRes = New cv.Size(640, 480)
            Case 7
                settings.workRes = New cv.Size(320, 240)
                settings.captureRes = New cv.Size(640, 480)
            Case 8
                settings.workRes = New cv.Size(160, 120)
                settings.captureRes = New cv.Size(640, 480)
            Case 9
                settings.workRes = New cv.Size(672, 376)
                settings.captureRes = New cv.Size(672, 376)
            Case 10
                settings.workRes = New cv.Size(336, 188)
                settings.captureRes = New cv.Size(672, 376)
            Case 11
                settings.workRes = New cv.Size(168, 94)
                settings.captureRes = New cv.Size(672, 376)
        End Select
    End Sub
    Public Sub jsonWrite()
        If TestAllButton.Text <> "Stop Test" Then ' don't save the algorithm name and group if "Test All" is running.
            settings.MainUI_AlgName = AvailableAlgorithms.Text
            settings.groupComboText = GroupComboBox.Text
        End If

        settings.locationMain = New cv.Vec4f(Me.Left, Me.Top, Me.Width, Me.Height)
        If camPic(0) IsNot Nothing Then
            ' used only when .snapCustom is true
            settings.displayRes = New cv.Size(camPic(0).Width, camPic(0).Height)
        End If
        If settings.translatorMode = "" Then settings.translatorMode = "VB.Net to C#"

        Dim setlist = New List(Of jsonClass.ApplicationStorage)
        setlist.Add(settings)
        jsonfs.Save(setlist)
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
    Private Sub TestAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
        TestAllButton.Image = If(TestAllButton.Text = "Test All", stopTestAll, testAllToolbarBitmap)
        If TestAllButton.Text = "Test All" Then
            Debug.WriteLine("Starting 'TestAll' overnight run.")
            AlgorithmTestAllCount = 0

            setupTestAll()

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
        Dim zedIndex = cameraNames.IndexOf("StereoLabs ZED 2/2i")
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
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim executingAssemblyPath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
        Dim exeDir = New DirectoryInfo(Path.GetDirectoryName(executingAssemblyPath))
        HomeDir = New DirectoryInfo(exeDir.FullName + "/../../../../../")
        Directory.SetCurrentDirectory(HomeDir.FullName)
        HomeDir = New DirectoryInfo("./")

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture
        Dim args() = Environment.GetCommandLineArgs()

        jsonRead()
        setupPath()
        camSwitch()

        ' currently the only commandline arg is the name of the algorithm to run.  Save it and continue...
        If args.Length > 1 Then
            Dim algorithm As String = "AddWeighted_PS.py"
            settings.groupComboText = "< All >"
            If args.Length > 2 Then ' arguments from python os.spawnv are passed as wide characters.  
                For i = 0 To args.Length - 1
                    algorithm += args(i)
                Next
            Else
                algorithm = args(1)
            End If
            Debug.WriteLine("'" + algorithm + "' was provided in the command line arguments to OpenCVB")
            If algorithm = "Pyglet_Image_PS.py" Then End
            externalPythonInvocation = True ' we don't need to start python because it started OpenCVB.
        End If

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

        Dim systemPath = Environment.GetEnvironmentVariable("Path")
        pythonPresent = InStr(systemPath.ToLower, "python")
        If pythonPresent = False Then
            MessageBox.Show("Python needs to be in the path in order to run all the algorithms written in python." + vbCrLf +
                       "That is how you control which version of python is active for OpenCVB." + vbCrLf +
                       "All Python algorithms will be disabled for now...")
        End If

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
            jsonWrite()
        End If

        AvailableAlgorithms.ComboBox.Select()

        fpsTimer.Enabled = True
        XYLoc.Text = "(x:0, y:0) - last click point at: (x:0, y:0)"
        XYLoc.Visible = True

        If settings.cameraFound Then
            paintNewImages = False
            newCameraImages = False
            If cameraTaskHandle Is Nothing Then
                cameraTaskHandle = New Thread(AddressOf CameraTask)
                cameraTaskHandle.Name = "Camera Task"
                cameraTaskHandle.Start()
            End If
            CameraSwitching.Text = settings.cameraName + " starting"
            While camera Is Nothing ' wait for camera to start...
                Application.DoEvents()
                Thread.Sleep(100)
            End While
        End If

        Debug.WriteLine("Main_Load complete.")
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
            jsonWrite()
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

        jsonWrite()
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
        jsonWrite()
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
        Debug.WriteLine("Starting algorithm " + AvailableAlgorithms.Text)
        testAllRunning = TestAllButton.Text = "Stop Test"
        saveAlgorithmName = AvailableAlgorithms.Text ' this tells the previous algorithmTask to terminate.

        Dim parms As New VBClasses.VBtask.algParms
        parms.fpsRate = settings.desiredFPS

        parms.testAllRunning = testAllRunning

        parms.externalPythonInvocation = externalPythonInvocation
        parms.showConsoleLog = settings.showConsoleLog

        parms.HomeDir = HomeDir.FullName
        parms.cameraName = settings.cameraName
        parms.cameraIndex = settings.cameraIndex

        parms.main_hwnd = Me.Handle
        parms.mainFormLocation = New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height)

        parms.workRes = settings.workRes
        parms.captureRes = settings.captureRes
        parms.displayRes = settings.displayRes
        parms.algName = AvailableAlgorithms.Text

        PausePlayButton.Image = PausePlay

        Thread.CurrentThread.Priority = ThreadPriority.Lowest
        algorithmTaskHandle = New Thread(AddressOf AlgorithmTask) ' <<<<<<<<<<<<<<<<<<<<<<<<< This starts the VB_Classes algorithm.
        AlgDescription.Text = ""
        algorithmTaskHandle.Name = AvailableAlgorithms.Text
        algorithmTaskHandle.SetApartmentState(ApartmentState.STA) ' this allows the algorithm task to display forms and react to input.
        algorithmTaskHandle.Start(parms)

        Debug.WriteLine("Main.StartAlgorithm completed.")
    End Sub
    Private Function setCalibData(cb As Object) As VBtask.cameraInfo
        Dim cbNew As New VBtask.cameraInfo
        cbNew.rgbIntrinsics.ppx = cb.rgbIntrinsics.ppx
        cbNew.rgbIntrinsics.ppy = cb.rgbIntrinsics.ppy
        cbNew.rgbIntrinsics.fx = cb.rgbIntrinsics.fx
        cbNew.rgbIntrinsics.fy = cb.rgbIntrinsics.fy
        cbNew.leftIntrinsics.ppx = cb.leftIntrinsics.ppx
        cbNew.leftIntrinsics.ppy = cb.leftIntrinsics.ppy
        cbNew.leftIntrinsics.fx = cb.leftIntrinsics.fx
        cbNew.leftIntrinsics.fy = cb.leftIntrinsics.fy
        cbNew.ColorToLeft_rotation = cb.ColorToLeft_rotation
        cbNew.ColorToLeft_translation = cb.ColorToLeft_translation
        cbNew.baseline = cb.baseline
        cbNew.LtoR_translation = cb.LtoR_translation
        cbNew.LtoR_rotation = cb.LtoR_rotation
        Return cbNew
    End Function
    Private Sub AlgorithmTask(ByVal parms As VBClasses.VBtask.algParms)
        If parms.algName = "" Then Exit Sub
        algorithmQueueCount += 1
        algorithmFPSrate = 0
        newCameraImages = False

        While 1
            If camera IsNot Nothing Then
                parms.calibData = setCalibData(camera.calibData)
                Exit While
            ElseIf saveAlgorithmName = "" Then
                Exit Sub
            End If
        End While

        ' the duration of any algorithm varies a lot so wait here if previous algorithm is not finished.
        SyncLock algorithmThreadLock
            algorithmQueueCount -= 1
            AlgorithmTestAllCount += 1
            drawRect = New cv.Rect
            task = New VBtask(parms)
            SyncLock trueTextLock
                trueData = New List(Of TrueText)
            End SyncLock

            task.lowResDepth = New cv.Mat(task.workRes, cv.MatType.CV_32F)
            task.lowResColor = New cv.Mat(task.workRes, cv.MatType.CV_32F)

            task.MainUI_Algorithm = algolist.createAlgorithm(parms.algName)

            ' You may land here when the Group x-reference file has not been updated recently.
            ' It is not updated on every run because it would make rerunning take too long.
            ' if you land here and you were trying a subset group of algorithms,
            ' then remove the json file and restart, click the OpenCVB options button,
            ' and click 'Update Algorithm XRef' (it is toward the bottom of the options form.)
            textDesc = task.MainUI_Algorithm.desc

            If ComplexityTimer.Enabled = False Then
                Debug.WriteLine(CStr(Now))
                Debug.WriteLine(vbCrLf + vbCrLf + vbTab + parms.algName + vbCrLf + vbTab +
                                      CStr(AlgorithmTestAllCount) + vbTab + "Algorithms tested")
                Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
                totalBytesOfMemoryUsed = currentProcess.WorkingSet64 / (1024 * 1024)
                Debug.WriteLine(vbTab + Format(totalBytesOfMemoryUsed, "#,##0") + "Mb working set before running " +
                                      parms.algName + " with " + CStr(Process.GetCurrentProcess().Threads.Count) + " threads")

                Debug.WriteLine(vbTab + "Active camera = " + settings.cameraName + ", Input resolution " +
                                      CStr(settings.captureRes.Width) + "x" + CStr(settings.captureRes.Height) + " and working resolution of " +
                                      CStr(settings.workRes.Width) + "x" + CStr(settings.workRes.Height) + vbCrLf)
            End If

            ' Adjust drawrect for the ratio of the actual size and workRes.
            If task.drawRect <> New cv.Rect Then
                ' relative size of algorithm size image to displayed image
                Dim ratio = camPic(0).Width / task.dst2.Width
                drawRect = New cv.Rect(task.drawRect.X * ratio, task.drawRect.Y * ratio,
                                        task.drawRect.Width * ratio, task.drawRect.Height * ratio)
            End If

            Dim saveworkRes = settings.workRes
            task.labels = {"", "", "", ""}
            mouseDisplayPoint = New cv.Point(task.dst2.Width / 2, task.dst2.Height / 2) ' mouse click point default = center of the image

            Dim saveDrawRect As cv.Rect
            task.motionMask = New cv.Mat(task.workRes, cv.MatType.CV_8U, 255)
            task.depthMaskRaw = New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
            While 1
                Dim waitTime = Now
                ' relative size of displayed image and algorithm size image.
                While 1
                    ' exit the inner while if any of these change.
                    If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Then Exit While
                    If saveworkRes <> settings.workRes Then Exit While
                    If saveCameraName <> settings.cameraName Then Exit While
                    If saveAlgorithmName <> task.algName Then Exit While

                    If pauseAlgorithmThread Then
                        task.paused = True
                        Exit While ' this is useful because the pixelviewer can be used if paused.
                    Else
                        task.paused = False
                    End If

                    If newCameraImages Then
                        newCameraImages = False
                        Dim copyTime = Now

                        SyncLock cameraLock
                            task.color = camera.uiColor
                            task.leftView = camera.uiLeft
                            task.rightView = camera.uiRight
                            ' make sure left and right views are present
                            If task.leftView.Width = 0 Then
                                task.leftView = New cv.Mat(task.color.Size, cv.MatType.CV_8U, 0)
                            End If
                            If task.rightView.Width = 0 Then
                                task.rightView = New cv.Mat(task.color.Size, cv.MatType.CV_8U, 0)
                            End If
                            task.pointCloud = camera.uiPointCloud

                            ' there might be a delay in the camera task so set it again here....
                            If frameCount <10 Then task.calibData= setCalibData(camera.calibData)

                            task.transformationMatrix= camera.transformationMatrix
                            task.IMU_TimeStamp= camera.IMU_TimeStamp
                            task.IMU_Acceleration= camera.IMU_Acceleration
                            task.IMU_AngularAcceleration= camera.IMU_AngularAcceleration
                            task.IMU_AngularVelocity= camera.IMU_AngularVelocity
                            task.IMU_FrameTime= camera.IMU_FrameTime
                            task.CPU_TimeStamp= camera.CPU_TimeStamp
                            task.CPU_FrameTime= camera.CPU_FrameTime
                        End SyncLock

                        Dim endCopyTime = Now
                        Dim elapsedCopyTicks = endCopyTime.Ticks - copyTime.Ticks
                        Dim spanCopy = New TimeSpan(elapsedCopyTicks)
                        task.inputBufferCopy = spanCopy.Ticks / TimeSpan.TicksPerMillisecond

                        If GrabRectangleData Then
                            GrabRectangleData = False
                            ' relative size of algorithm size image to displayed image
                            Dim ratio = task.dst2.Width / camPic(0).Width
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

                        Exit While
                    End If
                End While

                ' exit the outer while if any of these change.
                If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Then Exit While
                If saveworkRes <> settings.workRes Then Exit While
                If saveCameraName <> settings.cameraName Then Exit While
                If saveAlgorithmName <> task.algName Then Exit While

                If activeMouseDown = False Then
                    SyncLock mouseLock
                        If mouseDisplayPoint.X < 0 Then mouseDisplayPoint.X = 0
                        If mouseDisplayPoint.Y < 0 Then mouseDisplayPoint.Y = 0
                        If mouseDisplayPoint.X >= task.dst2.Width Then mouseDisplayPoint.X = task.dst2.Width - 1
                        If mouseDisplayPoint.Y >= task.dst2.Height Then mouseDisplayPoint.Y = task.dst2.Height - 1

                        task.mouseMovePoint = mouseDisplayPoint
                        If task.mouseMovePoint = New cv.Point(0, 0) Then
                            task.mouseMovePoint = New cv.Point(task.dst2.Width / 2, task.dst2.Height / 2)
                        End If
                        task.mouseMovePoint = validatePoint(task.mouseMovePoint)
                        task.mousePicTag = mousePicTag
                        If task.ClickPoint = New cv.Point Then task.ClickPoint = New cv.Point(task.workRes.Width / 2, task.workRes.Height / 2)
                        If mouseClickFlag Then
                            task.mouseClickFlag = mouseClickFlag
                            task.ClickPoint = mouseDisplayPoint
                            ClickPoint = task.ClickPoint
                            mouseClickFlag = False
                        End If
                    End SyncLock
                End If

                If activateTaskForms Then
                    task.activateTaskForms = True
                    activateTaskForms = False
                End If

                Dim endWaitTime = Now
                Dim elapsedWaitTicks = endWaitTime.Ticks - waitTime.Ticks
                Dim spanWait = New TimeSpan(elapsedWaitTicks)
                task.waitingForInput = spanWait.Ticks / TimeSpan.TicksPerMillisecond - task.inputBufferCopy
                Dim updatedDrawRect = task.drawRect
                task.fpsCamera = fpsCamera

                If testAllRunning Then
                    task.pixelViewerOn = False
                Else
                    task.pixelViewerOn = pixelViewerOn
                End If



                task.RunAlgorithm() ' <<<<<<<<<<< this is where the real work gets done.




                picLabels = task.labels
                motionLabel = task.motionLabel

                SyncLock mouseLock
                    mouseDisplayPoint = validatePoint(mouseDisplayPoint)
                    mouseMovePoint = mouseMovePoint
                End SyncLock

                Dim returnTime = Now

                ' in case the algorithm has changed the mouse location...
                If task.mouseMovePointUpdated Then mouseDisplayPoint = task.mouseMovePoint
                If updatedDrawRect <> task.drawRect Then
                    drawRect = task.drawRect
                    ' relative size of algorithm size image to displayed image
                    Dim ratio = camPic(0).Width / task.dst2.Width
                    drawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                End If
                If task.drawRectClear Then
                    drawRect = New cv.Rect
                    task.drawRect = drawRect
                    task.drawRectClear = False
                End If

                pixelViewerRect = task.pixelViewerRect
                pixelViewTag = task.pixelViewTag

                If Single.IsNaN(algorithmFPSrate) Then
                    task.fpsAlgorithm = 0
                Else
                    task.fpsAlgorithm = If(algorithmFPSrate < 0.01, 0, algorithmFPSrate)
                End If

                Dim ptCursor As New cv.Point
                Dim ptM = task.mouseMovePoint, w = task.workRes.Width, h = task.workRes.Height
                If ptM.X >= 0 And ptM.X < w And ptM.Y >= 0 And ptM.Y < h Then
                    ptCursor = validatePoint(task.mouseMovePoint)
                    SyncLock trueTextLock
                        trueData.Clear()
                        If task.trueData.Count Then
                            trueData = New List(Of VBClasses.TrueText)(task.trueData)
                        End If
                        If task.paused = False Then
                            trueData.Add(New TrueText(task.depthAndCorrelationText, New cv.Point(ptM.X, ptM.Y - 24), 1))
                        End If
                        task.trueData.Clear()
                    End SyncLock
                End If

                If task.displayDst1 = False Or task.labels(1) = "" Then picLabels(1) = "DepthRGB"
                picLabels(1) = task.depthAndCorrelationText.Replace(vbCrLf, "")

                If task.dst0 IsNot Nothing Then
                    SyncLock cameraLock
                        dst(0) = task.dst0.Clone
                        dst(1) = task.dst1.Clone
                        dst(2) = task.dst2.Clone
                        dst(3) = task.dst3.Clone
                        paintNewImages = True ' trigger the paint 
                    End SyncLock
                    algorithmRefresh = True
                End If

                dst(0).Circle(ptCursor, task.DotSize + 1, cv.Scalar.White, -1)
                dst(1).Circle(ptCursor, task.DotSize + 1, cv.Scalar.White, -1)
                dst(2).Circle(ptCursor, task.DotSize + 1, cv.Scalar.White, -1)
                dst(3).Circle(ptCursor, task.DotSize + 1, cv.Scalar.White, -1)

                If task.fpsAlgorithm = 0 Then task.fpsAlgorithm = 1

                Dim elapsedTicks = Now.Ticks - returnTime.Ticks
                Dim span = New TimeSpan(elapsedTicks)
                task.returnCopyTime = span.Ticks / TimeSpan.TicksPerMillisecond

                task.mouseClickFlag = False
                frameCount = task.frameCount
                ' this can be very useful.  When debugging your algorithm, turn this global option on to sync output to debug.
                ' Each image will represent the one just finished by the algorithm.
                If task.debugSyncUI Then Thread.Sleep(100)
                If task.closeRequest Then End
            End While

            Debug.WriteLine(parms.algName + " ending.  Thread closing...")
            task.frameCount = -1
            Application.DoEvents()
            task.Dispose()
        End SyncLock

        If parms.algName.EndsWith(".py") Then killThread("python")
        frameCount = 0
    End Sub
End Class
