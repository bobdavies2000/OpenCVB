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
    'Dim algolist As algorithmList = New algorithmList
    'Public Shared settings As jsonClass.ApplicationStorage
    'Dim jsonfs As New jsonClass.FileOperations
    'Dim optionsForm As Options
    'Dim trueData As New List(Of TrueText)

    Public HomeDir As DirectoryInfo
    Public Shared cameraNames As List(Of String)
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
    Dim groupList As New List(Of String)
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
    Public Sub jsonRead()
        'jsonfs.jsonFileName = HomeDir.FullName + "settings.json"
        'settings = jsonfs.Load()(0)
        '' The camera names are defined in VBtask.algParms.cameraNames
        '' the 3 lists below must have an entry for each camera - supported/640x480/1920...
        ''  cameraNames = New List(Of String)(VB_Classes.VBtask.algParms.cameraNames)  ' <<<<<<<<<<<< here is the list of supported cameras.
        'With settings
        '    ' copies here for convenience.
        '    'Public Shared cameraNames As New List(Of String)({"StereoLabs ZED 2/2i",
        '    '                                              "Orbbec Gemini 335L",
        '    '                                              "Orbbec Gemini 336L",
        '    '                                              "Oak-D camera",
        '    '                                              "Intel(R) RealSense(TM) Depth Camera 435i",
        '    '                                              "Intel(R) RealSense(TM) Depth Camera 455",
        '    '                                              "MYNT-EYE-D1000",
        '    '                                              "Orbbec Gemini 335"})
        '    ' Mynt support updated below
        '    .cameraSupported = New List(Of Boolean)({True, True, True, True, True, False, True, True})
        '    .camera640x480Support = New List(Of Boolean)({False, True, True, False, False, False, True, True})
        '    .camera1920x1080Support = New List(Of Boolean)({True, False, False, False, True, False, False, False})
        '    Dim defines = New FileInfo(HomeDir.FullName + "Cameras\CameraDefines.hpp")
        '    Dim stereoLabsDefineIsOff As Boolean
        '    Dim sr = New StreamReader(defines.FullName)
        '    Dim zedIndex = cameraNames.IndexOf("StereoLabs ZED 2/2i")
        '    Dim myntIndex = cameraNames.IndexOf("MYNT-EYE-D1000")
        '    While sr.EndOfStream = False
        '        Dim nextLine = sr.ReadLine
        '        If nextLine.StartsWith("#define MYNTD_1000") Then
        '            .cameraSupported(myntIndex) = True
        '        End If
        '        If nextLine.Contains("STEREOLAB") Then
        '            If nextLine.StartsWith("//") Then
        '                .cameraSupported(zedIndex) = False
        '                stereoLabsDefineIsOff = True
        '            End If
        '        End If
        '    End While

        '    sr.Close()

        '    ' checking the list for specific missing device here...
        '    Dim usbList = USBenumeration()
        '    Dim testlist As New List(Of String)
        '    For Each usbDevice In usbList
        '        If LCase(usbDevice).Contains("orb") Then testlist.Add(usbDevice) ' debugging assistance...
        '    Next

        '    .cameraPresent = New List(Of Boolean)
        '    For i = 0 To cameraNames.Count - 1
        '        Dim searchname = cameraNames(i)
        '        Dim present As Boolean = False
        '        If searchname.Contains("Oak-D") Then searchname = "Movidius MyriadX"
        '        If stereoLabsDefineIsOff = False Then
        '            If searchname.StartsWith("StereoLabs ZED 2/2i") Then searchname = "ZED 2"
        '        End If

        '        Dim subsetList As New List(Of String)
        '        For Each usbDevice In usbList
        '            If usbDevice.Contains("Orb") Then subsetList.Add(usbDevice)
        '            If usbDevice.Contains(searchname) Then present = True
        '        Next
        '        .cameraPresent.Add(present <> False)
        '    Next

        '    For i = 0 To cameraNames.Count - 1
        '        If cameraNames(i).StartsWith("Orbbec") Then
        '            If cameraNames(i) = .cameraName Then
        '                .cameraIndex = i
        '                Exit For
        '            End If
        '        Else
        '            If cameraNames(i).Contains(.cameraName) And .cameraName <> "" Then
        '                .cameraIndex = i
        '                Exit For
        '            End If
        '        End If
        '    Next

        '    If .cameraName = "" Or .cameraPresent(.cameraIndex) = False Then
        '        For i = 0 To cameraNames.Count - 1
        '            If .cameraPresent(i) Then
        '                .cameraIndex = i
        '                .cameraName = cameraNames(i)
        '                Exit For
        '            End If
        '        Next
        '    Else
        '        For i = 0 To cameraNames.Count - 1
        '            If cameraNames(i) = .cameraName Then .cameraIndex = i
        '        Next
        '    End If

        '    If .cameraPresent(myntIndex) And .cameraSupported(myntIndex) = False Then
        '        MsgBox("A MYNT D 1000 camera is present but OpenCVB's" + vbCrLf +
        '               "Cam_MyntD.dll has not been built." + vbCrLf + vbCrLf +
        '               "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
        '               "and run AddMynt.bat in OpenCVB's home directory.")
        '    End If

        '    If .cameraPresent(zedIndex) And .cameraSupported(zedIndex) = False And stereoLabsDefineIsOff = False Then
        '        MsgBox("A StereoLabls ZED 2 camera is present but OpenCVB's" + vbCrLf +
        '               "Cam_Zed2.dll has not been built with the SDK." + vbCrLf + vbCrLf +
        '               "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
        '               "and rerun Update_All.bat to get the StereoLabs SDK.")
        '    End If

        '    settings.cameraFound = False
        '    For i = 0 To settings.cameraPresent.Count - 1
        '        If settings.cameraPresent(i) Then
        '            settings.cameraFound = True
        '            Exit For
        '        End If
        '    Next
        '    If settings.cameraFound = False Then
        '        settings.cameraName = ""
        '        MsgBox("There are no supported cameras present!" + vbCrLf + vbCrLf)
        '    End If

        '    If settings.testAllDuration < 5 Then settings.testAllDuration = 5
        '    If settings.fontInfo Is Nothing Then settings.fontInfo = New Font("Tahoma", 9)

        '    If .workRes = New cv.Size Then .workRes = New cv.Size(640, 480)
        '    Select Case .workRes.Height
        '        Case 270, 540, 1080
        '            .captureRes = New cv.Size(1920, 1080)
        '            If .camera1920x1080Support(.cameraIndex) = False Then
        '                .captureRes = New cv.Size(1280, 720)
        '                .workRes = New cv.Size(320, 180)
        '            End If
        '        Case 180, 360, 720
        '            .captureRes = New cv.Size(1280, 720)
        '        Case 376, 188, 94
        '            If settings.cameraName <> "StereoLabs ZED 2/2i" Then
        '                MsgBox("The json settings don't appear to be correct!" + vbCrLf +
        '                        "The 'settings.json' file will be removed" + vbCrLf +
        '                        "and rebuilt with default settings upon restart.")
        '                Dim fileinfo As New FileInfo(jsonfs.jsonFileName)
        '                fileinfo.Delete()
        '                End
        '            End If
        '            .captureRes = New cv.Size(672, 376)
        '        Case 120, 240, 480
        '            .captureRes = New cv.Size(640, 480)
        '            If .camera640x480Support(.cameraIndex) = False Then
        '                .captureRes = New cv.Size(1280, 720)
        '                .workRes = New cv.Size(320, 180)
        '            End If
        '    End Select

        '    Dim wh = .workRes.Height
        '    ' desktop style is the default
        '    If .snap320 = False And .snap640 = False And .snapCustom = False Then .snap640 = True
        '    If .snap640 Then
        '        .locationMain.Item2 = 1321
        '        .locationMain.Item3 = 870
        '        If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 1096
        '        If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cv.Size(640, 480) Else .displayRes = New cv.Size(640, 360)
        '    ElseIf .snap320 Then
        '        .locationMain.Item2 = 683
        '        .locationMain.Item3 = 510
        '        If wh = 240 Or wh = 480 Or wh = 120 Then .locationMain.Item3 = 616
        '        If wh = 240 Or wh = 480 Or wh = 120 Then .displayRes = New cv.Size(320, 240) Else .displayRes = New cv.Size(320, 180)
        '    End If

        '    Dim border As Integer = 6
        '    Dim defaultWidth = .workRes.Width * 2 + border * 7
        '    Dim defaultHeight = .workRes.Height * 2 + ToolStrip1.Height + border * 12
        '    If Me.Height < 50 Then
        '        Me.Width = defaultWidth
        '        Me.Height = defaultHeight
        '    End If

        '    If .fontInfo Is Nothing Then .fontInfo = New Font("Tahoma", 9)
        '    If settings.groupComboText = "" Then settings.groupComboText = "< All >"

        '    If testAllRunning = False Then
        '        Dim resStr = CStr(.workRes.Width) + "x" + CStr(.workRes.Height)
        '        For i = 0 To Options.resolutionList.Count - 1
        '            If Options.resolutionList(i).StartsWith(resStr) Then
        '                .workResIndex = i
        '                Exit For
        '            End If
        '        Next
        '    End If

        '    .desiredFPS = 60
        '    Me.Left = .locationMain.Item0
        '    Me.Top = .locationMain.Item1
        '    Me.Width = .locationMain.Item2
        '    Me.Height = .locationMain.Item3

        '    optionsForm = New Options
        '    optionsForm.defineCameraResolutions(settings.cameraIndex)
        'End With
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
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim executingAssemblyPath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
        Dim exeDir = New DirectoryInfo(Path.GetDirectoryName(executingAssemblyPath))
        HomeDir = New DirectoryInfo(exeDir.FullName + "/../../../../../")
        Directory.SetCurrentDirectory(HomeDir.FullName)
        HomeDir = New DirectoryInfo("./")

        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture
        Dim args() = Environment.GetCommandLineArgs()
    End Sub
End Class
