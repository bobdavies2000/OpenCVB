Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Runtime
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Threading
Imports cv = OpenCvSharp
'Imports VB_Classes
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
    'Dim trueData As New List(Of TrueText)
    'Dim algolist As algorithmList = New algorithmList
    Public Shared settings As jsonClass.ApplicationStorage
    Dim jsonfs As New jsonClass.FileOperations
    Dim optionsForm As Options

    Public HomeDir As DirectoryInfo
    Public groupButtonSelection As String
    Dim upArrow As Boolean, downArrow As Boolean

    Dim pathList As New List(Of String)

    Dim AlgorithmTestAllCount As Integer
    Dim algorithmCount As Integer
    Dim algorithmTaskHandle As Thread
    Dim algorithmQueueCount As Integer

    Dim saveAlgorithmName As String
    Dim shuttingDown As Boolean

    Dim BothFirstAndLastReady As Boolean

    Dim camera As Object
    Dim restartCameraRequest As Boolean

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
    Dim magIndex As Integer
    Dim motionLabel As String

    Dim depthAndCorrelationText As String
    Public Shared cameraNames As New List(Of String)({"StereoLabs ZED 2/2i",
                                                  "Orbbec Gemini 335L",
                                                  "Orbbec Gemini 336L",
                                                  "Oak-D camera",
                                                  "Intel(R) RealSense(TM) Depth Camera 435i",
                                                  "Intel(R) RealSense(TM) Depth Camera 455",
                                                  "MYNT-EYE-D1000",
                                                  "Orbbec Gemini 335"})

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
            Dim myntIndex = cameraNames.IndexOf("MYNT-EYE-D1000")
            While sr.EndOfStream = False
                Dim nextLine = sr.ReadLine
                If nextLine.StartsWith("#define MYNTD_1000") Then
                    .cameraSupported(myntIndex) = True
                End If
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

            If .cameraPresent(myntIndex) And .cameraSupported(myntIndex) = False Then
                MessageBox.Show("A MYNT D 1000 camera is present but OpenCVB's" + vbCrLf +
                       "Cam_MyntD.dll has not been built." + vbCrLf + vbCrLf +
                       "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                       "and run AddMynt.bat in OpenCVB's home directory.")
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
                .locationMain.Item3 = 870
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

        'If OKcancel = DialogResult.OK Then
        '    Task.optionsChanged = True
        '    If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e)
        '    saveAlgorithmName = ""
        '    settings.workRes = optionsForm.cameraworkRes
        '    settings.displayRes = optionsForm.cameraDisplayRes
        '    settings.cameraName = optionsForm.cameraName
        '    settings.cameraIndex = optionsForm.cameraIndex
        '    settings.testAllDuration = optionsForm.testDuration

        '    frameCount = 0
        '    setupCamPics()

        '    camSwitch()

        '    jsonWrite()
        '    jsonRead() ' this will apply all the changes...
        '    restartCameraRequest = True
        '    Application.DoEvents()

        '    StartTask()
        'Else
        '    settings.cameraIndex = saveCameraIndex
        'End If
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
        TestAllButton.Image = If(TestAllButton.Text = "Test All", stopTest, testAllToolbarBitmap)
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
            updatePath(HomeDir.FullName + "zed-c-api/Build/Release", "StereoLabs Zed 2i camera support of C# interface.")
            updatePath(HomeDir.FullName + "zed-c-api/Build/Debug", "StereoLabs Zed 2i camera support of C# interface.")
        End If
        updatePath(HomeDir.FullName + "OrbbecSDK\lib\win_x64\", "Orbbec camera support.")
        updatePath(HomeDir.FullName + "OrbbecSDK_CSharp\Build\Debug\", "Orbbec camera support.")
        updatePath(HomeDir.FullName + "OrbbecSDK_CSharp\Build\Release\", "Orbbec camera support.")
        updatePath(HomeDir.FullName + "OrbbecSDK\lib\win_x64\", "OrbbecSDK.dll")

        updatePath(HomeDir.FullName + "librealsense\build\Debug\", "Realsense camera support.")
        updatePath(HomeDir.FullName + "librealsense\build\Release\", "Realsense camera support.")

#If AZURE_SUPPORT Then
        updatePath(HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Release\", "Kinect camera support.")
        updatePath(HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Debug\", "Kinect camera support.")
#End If

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
        StartTask()
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
    Private Sub StartTask()
        Debug.WriteLine("Starting algorithm " + AvailableAlgorithms.Text)
        'testAllRunning = TestAllButton.Text = "Stop Test"
        'saveAlgorithmName = AvailableAlgorithms.Text ' this tells the previous algorithmTask to terminate.

        'Dim parms As New VB_Classes.VBtask.algParms
        'parms.fpsRate = settings.desiredFPS

        'parms.useRecordedData = GroupComboBox.Text = "<All using recorded data>"
        'parms.testAllRunning = testAllRunning

        'parms.externalPythonInvocation = externalPythonInvocation
        'parms.showConsoleLog = settings.showConsoleLog

        'parms.HomeDir = HomeDir.FullName
        'parms.cameraName = settings.cameraName
        'parms.cameraIndex = settings.cameraIndex

        'parms.main_hwnd = Me.Handle
        'parms.mainFormLocation = New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height)

        'parms.workRes = settings.workRes
        'parms.captureRes = settings.captureRes
        'parms.displayRes = settings.displayRes
        'parms.algName = AvailableAlgorithms.Text

        'PausePlayButton.Image = PausePlay

        'Thread.CurrentThread.Priority = ThreadPriority.Lowest
        'algorithmTaskHandle = New Thread(AddressOf AlgorithmTask) ' <<<<<<<<<<<<<<<<<<<<<<<<< This starts the VB_Classes algorithm.
        'AlgDescription.Text = ""
        'algorithmTaskHandle.Name = AvailableAlgorithms.Text
        'algorithmTaskHandle.SetApartmentState(ApartmentState.STA) ' this allows the algorithm task to display forms and react to input.
        'algorithmTaskHandle.Start(parms)

        Debug.WriteLine("Main_UI.StartTask completed.")
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
    'Private Sub AlgorithmTask(ByVal parms As VB_Classes.VBtask.algParms)
    '    If parms.algName = "" Then Exit Sub
    '    algorithmQueueCount += 1
    '    algorithmFPSrate = 0
    '    newCameraImages = False

    '    While 1
    '        If camera IsNot Nothing Then
    '            parms.calibData = camera.calibData
    '            Exit While
    '        ElseIf saveAlgorithmName = "" Then
    '            Exit Sub
    '        End If
    '    End While

    '    ' the duration of any algorithm varies a lot so wait here if previous algorithm is not finished.
    '    SyncLock algorithmThreadLock
    '        algorithmQueueCount -= 1
    '        AlgorithmTestAllCount += 1
    '        drawRect = New cv.Rect
    '        Task = New VBtask(parms)
    '        SyncLock trueTextLock
    '            trueData = New List(Of TrueText)
    '        End SyncLock

    '        Task.lowResDepth = New cv.Mat(Task.workRes, cv.MatType.CV_32F)
    '        Task.lowResColor = New cv.Mat(Task.workRes, cv.MatType.CV_32F)

    '        Task.MainUI_Algorithm = algolist.createAlgorithm(parms.algName)

    '        ' You may land here when the Group x-reference file has not been updated recently.
    '        ' It is not updated on every run because it would make rerunning take too long.
    '        ' if you land here and you were trying a subset group of algorithms,
    '        ' then remove the json file and restart, click the OpenCVB options button,
    '        ' and click 'Update Algorithm XRef' (it is toward the bottom of the options form.)
    '        textDesc = Task.MainUI_Algorithm.desc

    '        If ComplexityTimer.Enabled = False Then
    '            Debug.WriteLine(CStr(Now))
    '            Debug.WriteLine(vbCrLf + vbCrLf + vbTab + parms.algName + vbCrLf + vbTab +
    '                                  CStr(AlgorithmTestAllCount) + vbTab + "Algorithms tested")
    '            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
    '            totalBytesOfMemoryUsed = currentProcess.WorkingSet64 / (1024 * 1024)
    '            Debug.WriteLine(vbTab + Format(totalBytesOfMemoryUsed, "#,##0") + "Mb working set before running " +
    '                                  parms.algName + " with " + CStr(Process.GetCurrentProcess().Threads.Count) + " threads")

    '            Debug.WriteLine(vbTab + "Active camera = " + settings.cameraName + ", Input resolution " +
    '                                  CStr(settings.captureRes.Width) + "x" + CStr(settings.captureRes.Height) + " and working resolution of " +
    '                                  CStr(settings.workRes.Width) + "x" + CStr(settings.workRes.Height) + vbCrLf)
    '        End If

    '        ' Adjust drawrect for the ratio of the actual size and workRes.
    '        If Task.drawRect <> New cv.Rect Then
    '            ' relative size of algorithm size image to displayed image
    '            Dim ratio = camPic(0).Width / Task.dst2.Width
    '            drawRect = New cv.Rect(Task.drawRect.X * ratio, Task.drawRect.Y * ratio,
    '                                    Task.drawRect.Width * ratio, Task.drawRect.Height * ratio)
    '        End If

    '        Dim saveworkRes = settings.workRes
    '        Task.labels = {"", "", "", ""}
    '        mouseDisplayPoint = New cv.Point(Task.dst2.Width / 2, Task.dst2.Height / 2) ' mouse click point default = center of the image

    '        Dim saveDrawRect As cv.Rect
    '        Task.motionMask = New cv.Mat(Task.workRes, cv.MatType.CV_8U, 255)
    '        Task.depthMaskRaw = New cv.Mat(Task.workRes, cv.MatType.CV_8U, 0)
    '        While 1
    '            Dim waitTime = Now
    '            ' relative size of displayed image and algorithm size image.
    '            While 1
    '                ' camera has exited or resolution is changed.
    '                If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Or
    '                        saveworkRes <> settings.workRes Then Exit While
    '                If saveAlgorithmName <> Task.algName Then Exit While
    '                ' switching camera resolution means stopping the current algorithm
    '                If saveworkRes <> settings.workRes Then Exit While

    '                If pauseAlgorithmThread Then
    '                    Task.paused = True
    '                    Exit While ' this is useful because the pixelviewer can be used if paused.
    '                Else
    '                    Task.paused = False
    '                End If

    '                If newCameraImages Then
    '                    newCameraImages = False
    '                    Dim copyTime = Now

    '                    SyncLock cameraLock
    '                        Task.color = camera.uiColor
    '                        Task.leftView = camera.uiLeft
    '                        Task.rightView = camera.uiRight
    '                        ' make sure left and right views are present
    '                        If Task.leftView.Width = 0 Then
    '                            Task.leftView = New cv.Mat(Task.color.Size, cv.MatType.CV_8U, 0)
    '                        End If
    '                        If Task.rightView.Width = 0 Then
    '                            Task.rightView = New cv.Mat(Task.color.Size, cv.MatType.CV_8U, 0)
    '                        End If
    '                        Task.pointCloud = camera.uiPointCloud

    '                        If frameCount < 10 Then Task.calibData = camera.calibdata

    '                        Task.transformationMatrix = camera.transformationMatrix
    '                        Task.IMU_TimeStamp = camera.IMU_TimeStamp
    '                        Task.IMU_Acceleration = camera.IMU_Acceleration
    '                        Task.IMU_AngularAcceleration = camera.IMU_AngularAcceleration
    '                        Task.IMU_AngularVelocity = camera.IMU_AngularVelocity
    '                        Task.IMU_FrameTime = camera.IMU_FrameTime
    '                        Task.CPU_TimeStamp = camera.CPU_TimeStamp
    '                        Task.CPU_FrameTime = camera.CPU_FrameTime
    '                    End SyncLock

    '                    Dim endCopyTime = Now
    '                    Dim elapsedCopyTicks = endCopyTime.Ticks - copyTime.Ticks
    '                    Dim spanCopy = New TimeSpan(elapsedCopyTicks)
    '                    Task.inputBufferCopy = spanCopy.Ticks / TimeSpan.TicksPerMillisecond

    '                    If GrabRectangleData Then
    '                        GrabRectangleData = False
    '                        ' relative size of algorithm size image to displayed image
    '                        Dim ratio = Task.dst2.Width / camPic(0).Width
    '                        Dim tmpDrawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
    '                        Task.drawRect = New cv.Rect
    '                        If tmpDrawRect.Width > 0 And tmpDrawRect.Height > 0 Then
    '                            If saveDrawRect <> tmpDrawRect Then
    '                                Task.optionsChanged = True
    '                                saveDrawRect = tmpDrawRect
    '                            End If
    '                            Task.drawRect = tmpDrawRect
    '                        End If
    '                        BothFirstAndLastReady = False
    '                    End If

    '                    Exit While
    '                End If
    '            End While

    '            ' when "testAll" is running and switching resolutions, the camera task may have switched to the new
    '            ' resolution but the task has not.  This catches that and will rebuild the task structure and start fresh.
    '            ' BTW: if you are ever stuck debugging this code, there is a conflict deep in the compiler with using the
    '            ' word "task" for the main OpenCVB variable. It only shows up here.  If you carefully change "task" to "aTask"
    '            ' throughout VB_Classes, it will make it easier to debug this while loop.  "task" is not a reserved work in VB.Net
    '            ' but is seems to act like it in main_UI.vb.  Using "task" instead of "aTask" is to be preferred - just simpler to type.
    '            If Task.color.Size <> saveworkRes Then Exit While

    '            ' camera has exited or resolution is changed.
    '            If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Or saveworkRes <> settings.workRes Or
    '                saveAlgorithmName <> Task.algName Then
    '                Exit While
    '            End If

    '            If activeMouseDown = False Then
    '                SyncLock mouseLock
    '                    If mouseDisplayPoint.X < 0 Then mouseDisplayPoint.X = 0
    '                    If mouseDisplayPoint.Y < 0 Then mouseDisplayPoint.Y = 0
    '                    If mouseDisplayPoint.X >= Task.dst2.Width Then mouseDisplayPoint.X = Task.dst2.Width - 1
    '                    If mouseDisplayPoint.Y >= Task.dst2.Height Then mouseDisplayPoint.Y = Task.dst2.Height - 1

    '                    Task.mouseMovePoint = mouseDisplayPoint
    '                    If Task.mouseMovePoint = New cv.Point(0, 0) Then
    '                        Task.mouseMovePoint = New cv.Point(Task.dst2.Width / 2, Task.dst2.Height / 2)
    '                    End If
    '                    Task.mouseMovePoint = validatePoint(Task.mouseMovePoint)
    '                    Task.mousePicTag = mousePicTag
    '                    If Task.ClickPoint = New cv.Point Then Task.ClickPoint = New cv.Point(Task.workRes.Width / 2, Task.workRes.Height / 2)
    '                    If mouseClickFlag Then
    '                        Task.mouseClickFlag = mouseClickFlag
    '                        Task.ClickPoint = mouseDisplayPoint
    '                        ClickPoint = Task.ClickPoint
    '                        mouseClickFlag = False
    '                    End If
    '                End SyncLock
    '            End If

    '            If activateTaskForms Then
    '                Task.activateTaskForms = True
    '                activateTaskForms = False
    '            End If

    '            Dim endWaitTime = Now
    '            Dim elapsedWaitTicks = endWaitTime.Ticks - waitTime.Ticks
    '            Dim spanWait = New TimeSpan(elapsedWaitTicks)
    '            Task.waitingForInput = spanWait.Ticks / TimeSpan.TicksPerMillisecond - Task.inputBufferCopy
    '            Dim updatedDrawRect = Task.drawRect
    '            Task.fpsCamera = fpsCamera

    '            If testAllRunning Then
    '                Task.pixelViewerOn = False
    '            Else
    '                Task.pixelViewerOn = pixelViewerOn
    '            End If



    '            Task.RunAlgorithm() ' <<<<<<<<<<< this is where the real work gets done.




    '            picLabels = Task.labels
    '            motionLabel = Task.motionLabel

    '            SyncLock mouseLock
    '                mouseDisplayPoint = validatePoint(mouseDisplayPoint)
    '                mouseMovePoint = mouseMovePoint
    '            End SyncLock

    '            Dim returnTime = Now

    '            ' in case the algorithm has changed the mouse location...
    '            If Task.mouseMovePointUpdated Then mouseDisplayPoint = Task.mouseMovePoint
    '            If updatedDrawRect <> Task.drawRect Then
    '                drawRect = Task.drawRect
    '                ' relative size of algorithm size image to displayed image
    '                Dim ratio = camPic(0).Width / Task.dst2.Width
    '                drawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
    '            End If
    '            If Task.drawRectClear Then
    '                drawRect = New cv.Rect
    '                Task.drawRect = drawRect
    '                Task.drawRectClear = False
    '            End If

    '            pixelViewerRect = Task.pixelViewerRect
    '            pixelViewTag = Task.pixelViewTag

    '            If Single.IsNaN(algorithmFPSrate) Then
    '                Task.fpsAlgorithm = 0
    '            Else
    '                Task.fpsAlgorithm = If(algorithmFPSrate < 0.01, 0, algorithmFPSrate)
    '            End If

    '            Dim ptCursor As New cv.Point
    '            Dim ptM = Task.mouseMovePoint, w = Task.workRes.Width, h = Task.workRes.Height
    '            If ptM.X >= 0 And ptM.X < w And ptM.Y >= 0 And ptM.Y < h Then
    '                ptCursor = validatePoint(Task.mouseMovePoint)
    '                SyncLock trueTextLock
    '                    trueData.Clear()
    '                    If Task.trueData.Count Then
    '                        trueData = New List(Of VB_Classes.TrueText)(Task.trueData)
    '                    End If
    '                    If Task.paused = False Then
    '                        trueData.Add(New TrueText(Task.depthAndCorrelationText, New cv.Point(ptM.X, ptM.Y - 24), 1))
    '                    End If
    '                    Task.trueData.Clear()
    '                End SyncLock
    '            End If

    '            If Task.displayDst1 = False Or Task.labels(1) = "" Then picLabels(1) = "DepthRGB"
    '            picLabels(1) = Task.depthAndCorrelationText.Replace(vbCrLf, "")

    '            If Task.dst0 IsNot Nothing Then
    '                SyncLock cameraLock
    '                    dst(0) = Task.dst0.Clone
    '                    dst(1) = Task.dst1.Clone
    '                    dst(2) = Task.dst2.Clone
    '                    dst(3) = Task.dst3.Clone
    '                    paintNewImages = True ' trigger the paint 
    '                End SyncLock
    '                algorithmRefresh = True
    '            End If

    '            dst(0).Circle(ptCursor, Task.DotSize + 1, cv.Scalar.White, -1)
    '            dst(1).Circle(ptCursor, Task.DotSize + 1, cv.Scalar.White, -1)
    '            dst(2).Circle(ptCursor, Task.DotSize + 1, cv.Scalar.White, -1)
    '            dst(3).Circle(ptCursor, Task.DotSize + 1, cv.Scalar.White, -1)

    '            If Task.fpsAlgorithm = 0 Then Task.fpsAlgorithm = 1

    '            Dim elapsedTicks = Now.Ticks - returnTime.Ticks
    '            Dim span = New TimeSpan(elapsedTicks)
    '            Task.returnCopyTime = span.Ticks / TimeSpan.TicksPerMillisecond

    '            Task.mouseClickFlag = False
    '            frameCount = Task.frameCount
    '            ' this can be very useful.  When debugging your algorithm, turn this global option on to sync output to debug.
    '            ' Each image will represent the one just finished by the algorithm.
    '            If Task.debugSyncUI Then Thread.Sleep(100)
    '            If Task.closeRequest Then End
    '        End While

    '        Debug.WriteLine(parms.algName + " ending.  Thread closing...")
    '        Task.frameCount = -1
    '        Application.DoEvents()
    '        Task.Dispose()
    '    End SyncLock

    '    If parms.algName.EndsWith(".py") Then killThread("python")
    '    frameCount = 0
    'End Sub
End Class
