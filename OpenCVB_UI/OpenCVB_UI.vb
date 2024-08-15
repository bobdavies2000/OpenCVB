Imports System.Threading
Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
'Imports System.Management
Imports System.Runtime.InteropServices
'Imports VB_Classes
'Imports System.Threading
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
    Dim groupNames1 As New List(Of String)
    Dim TreeViewDialog As TreeviewForm
    Public algorithmFPS As Single
    Dim picLabels() = {"", "", "", ""}
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen
    Dim textDesc As String = ""
    Dim textAdvice As String = ""
    Dim totalBytesOfMemoryUsed As Integer
    Dim trueData As New List(Of VB_Classes.trueText)
    ' Dim mbuf(2 - 1) As VB_Classes.VBtask.inBuffer
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

            If TestAll = False Then
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

    End Sub
End Class
