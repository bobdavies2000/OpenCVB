﻿Imports System.ComponentModel
Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Imports System.Management
Imports System.Runtime.InteropServices
Imports NAudio

Module opencv_module
    ' Public bufferLock As New Mutex(True, "bufferLock") ' this is a global lock on the camera buffers.
    Public callTraceLock As New Mutex(True, "callTraceLock")
    Public mouseLock As New Mutex(True, "mouseLock") ' global lock for use with mouse clicks.
    Public algorithmThreadLock As New Mutex(True, "AlgorithmThreadLock")
    Public cameraLock As New Mutex(True, "cameraLock")
    Public paintLock As New Mutex(True, "paintLock")
    Public Declare Function GetWindowRect Lib "user32" (ByVal HWND As Integer, ByRef lpRect As RECT) As Integer
    <StructLayout(LayoutKind.Sequential)> Public Structure RECT
        Dim Left As Integer
        Dim Top As Integer
        Dim Right As Integer
        Dim Bottom As Integer
    End Structure
End Module
Public Class OpenCVB
#Region "Globals"
    Dim mbuf(2 - 1) As VB_Classes.VBtask.inBuffer
    Dim mbIndex As Integer

    Dim optionsForm As OptionsDialog
    Dim AlgorithmCount As Integer
    Dim AlgorithmTestAllCount As Integer
    Dim algorithmTaskHandle As Thread
    Dim algorithmQueueCount As Integer

    Dim saveAlgorithmName As String
    Dim shuttingDown As Boolean
    Const minFrames = 2 ' must have this many frames before algorithm or camera can be changed...

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
    Dim clickPoint As New cv.Point
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
    Dim totalBytesOfMemoryUsed As Integer
    Dim trueData As List(Of VB_Classes.trueText)

    Dim pauseAlgorithmThread As Boolean
    Dim logAlgorithms As StreamWriter
    Public callTrace As New List(Of String)
    Public algorithm_ms As New List(Of Single)
    Public algorithmNames As New List(Of String)

    Const MAX_RECENT = 25
    Dim algHistory As New List(Of String)
    Dim arrowList As New List(Of String)
    Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
    Dim arrowIndex As Integer

    Public intermediateReview As String
    Dim activateTaskRequest As Boolean
    Dim pixelViewerRect As cv.Rect
    Dim pixelViewTag As Integer

    Dim PausePlay As Bitmap
    Dim runPlay As Bitmap
    Dim stopTest As Bitmap
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
            .cameraSupported = New List(Of Boolean)({True, True, True, True, True, False}) ' Zed and Mynt updated below if supported
            .camera640x480Support = New List(Of Boolean)({False, True, True, False, False, False})
            .camera1920x1080Support = New List(Of Boolean)({True, False, False, False, True, False})
            Dim defines = New FileInfo(HomeDir.FullName + "Cameras\CameraDefines.hpp")
            Dim sr = New StreamReader(defines.FullName)
            If Trim(sr.ReadLine).StartsWith("//#define STEREOLAB_INSTALLED") = False Then .cameraSupported(4) = True
            If Trim(sr.ReadLine).StartsWith("//#define MYNTD_1000") = False Then .cameraSupported(5) = True
            sr.Close()

            .cameraPresent = New List(Of Boolean)
            For i = 0 To cameraNames.Count - 1
                Dim present = USBenumeration(cameraNames(i))
                If cameraNames(i).Contains("Oak-D") Then present = USBenumeration("Movidius MyriadX")
                If cameraNames(i).Contains("StereoLabs ZED 2/2i") Then present = USBenumeration("ZED 2i")
                If present = False And cameraNames(i).Contains("StereoLabs ZED 2/2i") Then present = USBenumeration("ZED 2") ' older edition.
                .cameraPresent.Add(present <> 0)
            Next

            For i = 0 To cameraNames.Count - 1
                If cameraNames(i) = .cameraName Then
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

            If .cameraName = "" Then
                Dim cameraList As String = ""
                For Each cam In cameraNames
                    cameraList += cam + vbCrLf
                Next
                MsgBox("There are no supported cameras present!" + vbCrLf + vbCrLf +
                       "Connect any of these cameras: " + vbCrLf + vbCrLf + cameraList)
            End If

            If settings.testAllDuration < 5 Then settings.testAllDuration = 5
            If settings.fontInfo Is Nothing Then settings.fontInfo = New Font("Tahoma", 9)

            Select Case .workingRes.Height
                Case 270, 540, 1080
                    .captureRes = New cv.Size(1920, 1080)
                    If .camera1920x1080Support(.cameraIndex) = False Then
                        .captureRes = New cv.Size(1280, 720)
                        .workingRes = New cv.Size(320, 180)
                    End If
                Case 180, 360, 720
                    .captureRes = New cv.Size(1280, 720)
                Case 376, 188, 94
                    .captureRes = New cv.Size(672, 376)
                Case 120, 240, 480
                    .captureRes = New cv.Size(640, 480)
                    If .camera640x480Support(.cameraIndex) = False Then
                        .captureRes = New cv.Size(1280, 720)
                        .workingRes = New cv.Size(320, 180)
                    End If
            End Select

            Dim wh = .workingRes.Height
            If .snap320 = False And .snap640 = False And .snapCustom = False Then .snap640 = True ' desktop style is the default
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
            Dim defaultWidth = .workingRes.Width * 2 + border * 7
            Dim defaultHeight = .workingRes.Height * 2 + ToolStrip1.Height + border * 12
            If Me.Height < 50 Then
                Me.Width = defaultWidth
                Me.Height = defaultHeight
            End If

            If .fontInfo Is Nothing Then .fontInfo = New Font("Tahoma", 9)
            If settings.algorithmGroup = "" Then settings.algorithmGroup = "<All but Python>"

            If testAllRunning = False Then
                Dim resStr = CStr(.workingRes.Width) + "x" + CStr(.workingRes.Height)
                For i = 0 To OptionsDialog.resolutionList.Count - 1
                    If OptionsDialog.resolutionList(i).StartsWith(resStr) Then
                        .workingResIndex = i
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
        settings.PixelViewerButton = PixelViewerButton.Checked
        If TreeViewDialog IsNot Nothing Then
            OpenCVB.settings.TreeLocX = TreeViewDialog.Left
            OpenCVB.settings.TreeLocY = TreeViewDialog.Top
            OpenCVB.settings.TreeLocHeight = TreeViewDialog.Height
        End If
        settings.displayRes = New cv.Size(camPic(0).Width, camPic(0).Height) ' used only when .snapCustom is true

        Dim setlist = New List(Of jsonClass.ApplicationStorage)
        setlist.Add(settings)
        jsonfs.Save(setlist)
    End Sub
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture
        Dim args() = Environment.GetCommandLineArgs()

        HomeDir = If(args.Length > 1, New DirectoryInfo(CurDir() + "\..\"), New DirectoryInfo(CurDir() + "\..\..\"))
        jsonRead()

        ' currently the only commandline arg is the name of the algorithm to run.  Save it and continue...
        If args.Length > 1 Then
            Dim algorithm As String = "z_AddWeighted_PS.py"
            settings.algorithmGroup = "<All>"
            If args.Length > 2 Then ' arguments from python os.spawnv are passed as wide characters.  
                For i = 0 To args.Length - 1
                    algorithm += args(i)
                Next
            Else
                algorithm = args(1)
            End If
            Console.WriteLine("'" + algorithm + "' was provided in the command line arguments to OpenCVB")
            If algorithm = "z_Pyglet_Image_PS.py" Then End
            externalPythonInvocation = True ' we don't need to start python because it started OpenCVB.
        End If

        PausePlay = New Bitmap(HomeDir.FullName + "OpenCVB/Data/PauseButton.png")
        stopTest = New Bitmap(HomeDir.FullName + "OpenCVB/Data/StopTest.png")
        testAllToolbarBitmap = New Bitmap(HomeDir.FullName + "OpenCVB/Data/testall.png")
        runPlay = New Bitmap(HomeDir.FullName + "OpenCVB/Data/PauseButtonRun.png")

        setupAlgorithmHistory()

        Dim libraries = {"Cam_K4A.dll", "Cam_RS2.dll", "CPP_Classes.dll", "Cam_MyntD.dll", "Cam_Zed2.dll"}
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

        startCamera()
        While camera Is Nothing
        End While

        setupCamPics()

        If settings.treeButton Then TreeButton_Click(sender, e)
        If settings.PixelViewerButton Then PixelViewerButton_Click(sender, e)

        loadAlgorithmComboBoxes()
        XYloc.Text = ""

        GroupName.Text = settings.algorithmGroup
        If AvailableAlgorithms.Items.Count = 0 Then GroupName.Text = "<All but Python>"
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

        setWindowsVersion()
        fpsTimer.Enabled = True
    End Sub
    Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim pic = DirectCast(sender, PictureBox)
        Dim ratio = camPic(2).Width / settings.workingRes.Width
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
                If camera.mbuf(mbIndex).color IsNot Nothing Then
                    If camera.mbuf(mbIndex).color.width > 0 And dst(0) IsNot Nothing Then
                        SyncLock paintLock
                            Dim camSize = New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height)
                            For i = 0 To dst.Count - 1
                                Dim tmp = dst(i).Resize(camSize)
                                cvext.BitmapConverter.ToBitmap(tmp, camPic(i).Image)
                            Next
                        End SyncLock
                    End If
                End If
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
                                     tt.x * ratio, tt.y * ratio)
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("Error in trueData update: " + ex.Message)
            End Try
        End SyncLock

        Dim workingRes = settings.workingRes
        Dim cres = settings.captureRes
        Dim dres = settings.displayRes
        Dim resolutionDetails = "Input " + CStr(cres.Width) + "x" + CStr(cres.Height) + ", Display " + CStr(dres.Width) + "x" + CStr(dres.Height) +
                                ", WorkingRes " + CStr(workingRes.Width) + "x" + CStr(workingRes.Height)
        If AvailableAlgorithms.Text.StartsWith("Related_") Then resolutionDetails = "" ' The Related algorithms need the space...
        camLabel(0).Text = "RGB"
        If picLabels(0) <> "" Then camLabel(0).Text = picLabels(0)
        If picLabels(1) <> "" Then camLabel(1).Text = picLabels(1)
        camLabel(2).Text = picLabels(2)
        camLabel(3).Text = picLabels(3)
        camLabel(0).Text += " - " + resolutionDetails
        If picLabels(1) = "" Or testAllRunning Then camLabel(1).Text = "Depth RGB"
        AlgorithmDesc.Text = textDesc
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
            jumpToAlgorithm(item.Name)
        End If
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        If arrowIndex = 0 Then
            arrowList.Clear()
            For i = 0 To algHistory.Count - 1
                arrowList.Add(algHistory.ElementAt(i))
            Next
        End If
        arrowIndex = Math.Min(arrowList.Count - 1, arrowIndex + 1)
        jumpToAlgorithm(arrowList.ElementAt(arrowIndex))
    End Sub
    Private Sub ToolStripButton2_Click(sender As Object, e As EventArgs) Handles ToolStripButton2.Click
        If arrowIndex = 0 Then
            jumpToAlgorithm(AvailableAlgorithms.Items(Math.Min(AvailableAlgorithms.Items.Count - 1, AvailableAlgorithms.SelectedIndex + 1)))
        Else
            arrowIndex = Math.Max(0, arrowIndex - 1)
            jumpToAlgorithm(arrowList.ElementAt(arrowIndex))
        End If
    End Sub
    Private Sub setupAlgorithmHistory()
        For i = 0 To MAX_RECENT - 1
            Dim nextA = GetSetting("OpenCVB1", "algHistory" + CStr(i), "algHistory" + CStr(i), "recent algorithm " + CStr(i))
            If nextA = "" Then Exit For
            If algHistory.Contains(nextA) = False Then
                algHistory.Add(nextA)
                recentMenu(i) = New ToolStripMenuItem() With {.Text = nextA, .Name = nextA}
                AddHandler recentMenu(i).Click, AddressOf algHistory_Clicked
                MainMenu.DropDownItems.Add(recentMenu(i))
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
        For i = 0 To algHistory.Count - 1
            If algHistory(i) <> "" Then
                If recentMenu(i) Is Nothing Then
                    recentMenu(i) = New ToolStripMenuItem() With {.Text = algHistory(i), .Name = algHistory(i)}
                    AddHandler recentMenu(i).Click, AddressOf algHistory_Clicked
                End If
                recentMenu(i).Text = algHistory(i)
                recentMenu(i).Name = algHistory(i)
                SaveSetting("OpenCVB1", "algHistory" + CStr(i), "algHistory" + CStr(i), algHistory(i))
            End If
        Next
    End Sub
    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("The objective is to solve many small computer vision problems " + vbCrLf +
                   "and do so in a way that enables any of the solutions to be reused." + vbCrLf +
               "The result is a toolkit for solving ever bigger and more difficult" + vbCrLf +
               "problems.  The hypothesis behind this approach is that human vision" + vbCrLf +
               "is not computationally intensive but is built on many almost trivial" + vbCrLf +
               "algorithms working together." + vbCrLf)
    End Sub
    Private Function killPythonCameraOrTask() As Boolean
        Dim proc = Process.GetProcesses()
        Dim foundCamera As Boolean
        For i = 0 To proc.Count - 1
            If proc(i).ProcessName.ToLower.Contains("python") Then
                If proc(i).HasExited = False Then
                    proc(i).Kill()
                    If proc(i).ProcessName.ToLower.Contains("pythonw") Then
                        Thread.Sleep(100) ' let the camera task free resources.
                        foundCamera = True
                    End If
                End If
            End If
        Next
        Return foundCamera
    End Function
    Private Sub killTranslator()
        Dim proc = Process.GetProcesses()
        For i = 0 To proc.Count - 1
            If proc(i).ProcessName.ToLower.Contains("vb_to_cpp") Then
                If proc(i).HasExited = False Then proc(i).Kill()
            End If
        Next
    End Sub
    Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        jsonWrite()

        cameraTaskHandle = Nothing
        If TreeButton.Checked Then TreeViewDialog.Close()

        killTranslator()

        saveAlgorithmName = "" ' this will close the current algorithm.
    End Sub
    Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs) Handles RefreshTimer.Tick
        If (paintNewImages Or algorithmRefresh) And saveAlgorithmName = AvailableAlgorithms.Text Then Me.Refresh()
    End Sub
    Private Sub setWindowsVersion()
        Dim Version = Environment.OSVersion.Version
        Console.WriteLine("Windows version = " + CStr(Version.Major) + "." + CStr(Version.Minor) + " with build = " + CStr(Version.Build))
        If Version.Build >= 22000 Then windowsVersion = 11 Else windowsVersion = 10
    End Sub
    Private Sub PixelViewerButton_Click(sender As Object, e As EventArgs) Handles PixelViewerButton.Click
        If fpsTimer.Enabled Then
            SaveSetting("OpenCVB1", "PixelViewerLeft", "PixelViewerLeft", Me.Left)
            SaveSetting("OpenCVB1", "PixelViewerTop", "PixelViewerTop", Me.Height)
            SaveSetting("OpenCVB1", "PixelViewerWidth", "PixelViewerWidth", Me.Width)
        End If
        PixelViewerButton.Checked = Not PixelViewerButton.Checked
        settings.PixelViewerButton = PixelViewerButton.Checked
    End Sub
    Private Sub loadAlgorithmComboBoxes()
        ' we always need the number of lines from the algorithmList.txt file (and it is not always read when working with a subset of algorithms.)
        Dim AlgorithmListFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmList.txt")
        If AlgorithmListFileInfo.Exists = False Then
            MsgBox("The AlgorithmList.txt file is missing.  It should be in " + AlgorithmListFileInfo.FullName + "  Look at UI_Generator project.")
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

        Dim AlgorithmMapFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmMapToOpenCV.txt")
        If AlgorithmMapFileInfo.Exists = False Then
            MsgBox("The AlgorithmMapToOpenCV.txt file is missing.  Look at the 'UIindexer' Project that creates the mapping of algorithms to OpenCV keywords.")
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
                        groupNames.Add("")
                        GroupName.Items.Add("")
                    End If
                    Dim nameSplit = Split(0).Split("_")
                    If nameSplit(0) <> lastNameSplit And lastNameSplit <> "" Then
                        groupNames.Add("")
                        GroupName.Items.Add("")
                    End If
                    lastNameSplit = nameSplit(0)
                    lastSplit0 = Split(0)
                ElseIf lastSplit0.Contains("_") Then
                    groupNames.Add("")
                    GroupName.Items.Add("")
                    lastNameSplit = Split(0)
                End If
            End If
            groupNames.Add(infoLine)
            GroupName.Items.Add(Split(0))
        End While
        sr.Close()
    End Sub
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
        If GroupName.Text = "<All>" Or GroupName.Text = "<All using recorded data>" Then
            Dim AlgorithmListFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmList.txt")
            Dim sr = New StreamReader(AlgorithmListFileInfo.FullName)

            Dim infoLine = sr.ReadLine
            Dim Split = Regex.Split(infoLine, "\W+")
            CodeLineCount = Split(1)
            AvailableAlgorithms.Items.Clear()
            While sr.EndOfStream = False
                infoLine = sr.ReadLine
                infoLine = UCase(Mid(infoLine, 1, 1)) + Mid(infoLine, 2)
                If infoLine.StartsWith("Options_") = False Then addNextAlgorithm(infoLine, lastNameSplit)
            End While
            sr.Close()
        Else
            AvailableAlgorithms.Enabled = False
            Dim keyIndex = GroupName.Items.IndexOf(GroupName.Text)
            Dim groupings = groupNames(keyIndex)
            Dim split = Regex.Split(groupings, ",")
            AvailableAlgorithms.Items.Clear()

            For i = 1 To split.Length - 1
                If split(i).StartsWith("Options_") = False Then addNextAlgorithm(split(i), lastNameSplit)
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
            StartAlgorithmTask()
            updateAlgorithmHistory()
        End If
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
    Public Function validateRect(r As cv.Rect, width As Integer, height As Integer) As cv.Rect
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
    Private Sub campic_Click(sender As Object, e As EventArgs)
        SyncLock mouseLock
            mouseClickFlag = True
        End SyncLock
    End Sub
    Private Sub camPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
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

    Private Sub camPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = Windows.Forms.MouseButtons.Right Then
                activeMouseDown = True
            End If
            If e.Button = Windows.Forms.MouseButtons.Left Then
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
    Private Sub camPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
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
            mousePoint *= settings.workingRes.Width / camPic(0).Width
            XYloc.Text = mousePoint.ToString
        Catch ex As Exception
            Console.WriteLine("Error in camPic_MouseMove: " + ex.Message)
        End Try
    End Sub
    Private Sub campic_DoubleClick(sender As Object, e As EventArgs)
        DrawingRectangle = False
    End Sub
    Private Sub PausePlayButton_Click(sender As Object, e As EventArgs) Handles PausePlayButton.Click
        Static saveTestAllState As Boolean
        Static algorithmRunning = True
        If PausePlayButton.Text = "Run" Then
            PausePlayButton.Text = "Pause"
            pauseAlgorithmThread = False
            If saveTestAllState Then testAllButton_Click(sender, e)
            PausePlayButton.Image = PausePlay
        Else
            PausePlayButton.Text = "Run"
            pauseAlgorithmThread = True
            saveTestAllState = TestAllTimer.Enabled
            If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
            PausePlayButton.Image = runPlay
        End If
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
                camPic(i).Size = New Size(settings.workingRes.Width, settings.workingRes.Height)
                Me.Controls.Add(camPic(i))
            Next
        End If
        LineUpCamPics()
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
        AlgorithmDesc.Width = Me.Width - AlgorithmDesc.Left - 50

        Dim width = CInt((Me.Width - 42) / 2)
        Dim height = settings.displayRes.Height
        If settings.snap640 Then width = 640
        If settings.snap320 Then width = 320
        If settings.snapCustom Then ' custom size - neither snap320 or snap640
            Dim ratio = settings.workingRes.Height / settings.workingRes.Width
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

        Static saveAAwidth = AvailableAlgorithms.Width
        Static saveKeyLeft = GroupName.Left
        If AvailableAlgorithms.Left + AvailableAlgorithms.Width + GroupName.Width > Me.Width Then
            AvailableAlgorithms.Width = (Me.Width - AvailableAlgorithms.Left) / 2
            GroupName.Width = AvailableAlgorithms.Width - 20
            GroupName.Left = AvailableAlgorithms.Left + AvailableAlgorithms.Width + 1
        ElseIf Me.Width > AvailableAlgorithms.Left + AvailableAlgorithms.Width * 2 Then
            AvailableAlgorithms.Width = saveAAwidth
            GroupName.Width = saveAAwidth
            GroupName.Left = saveKeyLeft
        End If

        XYloc.Location = New Point(camPic(2).Left, camPic(2).Top + camPic(2).Height)
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

            Me.Text = "OpenCVB - " + Format(CodeLineCount, "###,##0") + " lines / " + CStr(AlgorithmCount) + " algorithms = " + CStr(CInt(CodeLineCount / AlgorithmCount)) +
                      " lines each (avg) - " + cameraName + " - " + Format(cameraFPS, "0.0") +
                      "/" + Format(algorithmFPS, "0.0")
        End If
    End Sub
    Private Sub OpenCVB_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        activateTaskRequest = True
    End Sub
    Private Sub ToolStripButton3_Click(sender As Object, e As EventArgs) Handles ToolStripButton3.Click
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
    Private Sub ToolStripButton4_Click(sender As Object, e As EventArgs) Handles ToolStripButton4.Click
        Shell(HomeDir.FullName + "bin/Debug/VB_to_CPP.exe", AppWinStyle.NormalFocus)
    End Sub
    Private Sub StartAlgorithmTask()
        Console.WriteLine("Starting algorithm " + AvailableAlgorithms.Text)
        SyncLock callTraceLock
            If TreeViewDialog IsNot Nothing Then
                algorithm_ms.Clear()
                TreeViewDialog.PercentTime.Text = ""
            End If
        End SyncLock
        testAllRunning = TestAllButton.Text = "Stop Test"
        saveAlgorithmName = AvailableAlgorithms.Text ' this tells the previous algorithmTask to terminate.

        Dim parms As New VB_Classes.VBtask.algParms
        parms.fpsRate = settings.desiredFPS
        parms.IMU_Present = True ' always present!

        parms.useRecordedData = GroupName.Text = "<All using recorded data>"
        parms.testAllRunning = testAllRunning

        parms.externalPythonInvocation = externalPythonInvocation
        parms.showConsoleLog = settings.showConsoleLog

        parms.homeDir = HomeDir.FullName
        parms.cameraName = settings.cameraName
        parms.cameraIndex = settings.cameraIndex
        parms.cameraInfo = camera.cameraInfo

        parms.main_hwnd = Me.Handle
        parms.mainFormLocation = New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height)

        parms.workingRes = settings.workingRes
        parms.captureRes = settings.captureRes
        parms.displayRes = settings.displayRes
        parms.algName = AvailableAlgorithms.Text

        PausePlayButton.Image = PausePlay

        ' If they Then had been Using the treeview feature To click On a tree entry, the timer was disabled.  
        ' Clicking on availablealgorithms indicates they are done with using the treeview.
        If TreeViewDialog IsNot Nothing Then TreeViewDialog.TreeViewTimer.Enabled = True

        Thread.CurrentThread.Priority = ThreadPriority.Lowest
        algorithmTaskHandle = New Thread(AddressOf AlgorithmTask)
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
            Dim task = New VB_Classes.VBtask(parms)
            textDesc = task.desc
            intermediateReview = ""

            Console.WriteLine(CStr(Now))
            Console.WriteLine(vbCrLf + vbCrLf + vbTab + parms.algName + " - " + textDesc + vbCrLf + vbTab +
                              CStr(AlgorithmTestAllCount) + vbTab + "Algorithms tested")
            Console.WriteLine(vbTab + Format(totalBytesOfMemoryUsed, "#,##0") + "Mb working set before running " +
                              parms.algName + " with " + CStr(Process.GetCurrentProcess().Threads.Count) + " threads")

            Console.WriteLine(vbTab + "Active camera = " + camera.cameraName + ", Input resolution " +
                              CStr(settings.captureRes.Width) + "x" + CStr(settings.captureRes.Height) + " and working resolution of " +
                              CStr(settings.workingRes.Width) + "x" + CStr(settings.workingRes.Height) + vbCrLf)

            ' Adjust drawrect for the ratio of the actual size and workingRes.
            If task.drawRect <> New cv.Rect Then
                ' relative size of algorithm size image to displayed image
                Dim ratio = camPic(0).Width / task.workingRes.Width
                drawRect = New cv.Rect(task.drawRect.X * ratio, task.drawRect.Y * ratio,
                                       task.drawRect.Width * ratio, task.drawRect.Height * ratio)
            End If

            SyncLock trueData
                trueData.Clear()
            End SyncLock

            BothFirstAndLastReady = False
            frameCount = 0 ' restart the count...

            RunTask(task)
            Console.WriteLine(parms.algName + " ending.  Thread closing...")
            task.Dispose()
        End SyncLock
        frameCount = 0
    End Sub
    Private Sub RunTask(task As VB_Classes.VBtask)
        Dim saveWorkingRes = settings.workingRes
        picLabels = {"", "", "", ""}
        task.labels = {"", "", "", ""}
        mousePoint = New cv.Point(task.workingRes.Width / 2, task.workingRes.Height / 2) ' mouse click point default = center of the image
        task.mouseClickFlag = True

        task.calibData.ppx = camera.cameraInfo.ppx
        task.calibData.ppy = camera.cameraInfo.ppy
        task.calibData.fx = camera.cameraInfo.fx
        task.calibData.fy = camera.cameraInfo.fy
        task.calibData.v_fov = camera.cameraInfo.v_fov
        task.calibData.h_fov = camera.cameraInfo.h_fov
        task.calibData.d_fov = camera.cameraInfo.d_fov

        task.pointCloud = mbuf(0).pointCloud
        task.color = mbuf(0).color

        While 1
            Dim waitTime = Now
            ' relative size of displayed image and algorithm size image.
            task.resolutionRatio = task.workingRes.Width / camPic(0).Width
            While 1
                ' camera has exited or resolution is changed.
                If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Or
                    saveWorkingRes <> settings.workingRes Then Exit While
                If frameCount > minFrames Then
                    If saveAlgorithmName <> task.algName Then Exit While
                    ' switching camera resolution means stopping the current algorithm
                    If saveWorkingRes <> settings.workingRes Then Exit While
                End If

                If pauseAlgorithmThread Then
                    Thread.Sleep(300)
                    task.paused = True
                    Exit While ' this is useful because the pixelviewer can be used if paused.
                Else
                    task.paused = False
                End If

                If newCameraImages Then
                    Dim copyTime = Now

                    SyncLock cameraLock
                        task.mbuf(mbIndex) = mbuf(mbIndex)
                        task.mbIndex = mbIndex
                        mbIndex += 1
                        If mbIndex >= mbuf.Count Then mbIndex = 0
                    End SyncLock

                    task.color = mbuf(mbIndex).color
                    task.leftView = mbuf(mbIndex).leftView
                    task.rightView = mbuf(mbIndex).rightView
                    task.pointCloud = mbuf(mbIndex).pointCloud

                    task.activateTaskRequest = activateTaskRequest
                    activateTaskRequest = False

                    Dim endCopyTime = Now
                    Dim elapsedCopyTicks = endCopyTime.Ticks - copyTime.Ticks
                    Dim spanCopy = New TimeSpan(elapsedCopyTicks)
                    task.inputBufferCopy = spanCopy.Ticks / TimeSpan.TicksPerMillisecond

                    task.transformationMatrix = camera.transformationMatrix
                    task.IMU_TimeStamp = camera.IMU_TimeStamp
                    task.IMU_Acceleration = camera.IMU_Acceleration
                    task.IMU_AngularAcceleration = camera.IMU_AngularAcceleration
                    task.IMU_AngularVelocity = camera.IMU_AngularVelocity
                    task.IMU_FrameTime = camera.IMU_FrameTime
                    task.CPU_TimeStamp = camera.CPU_TimeStamp
                    task.CPU_FrameTime = camera.CPU_FrameTime
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
                        Dim ratio = task.workingRes.Width / camPic(0).Width
                        Dim tmpDrawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio,
                                                      drawRect.Width * ratio, drawRect.Height * ratio)
                        task.drawRect = New cv.Rect
                        If tmpDrawRect.Width > 0 And tmpDrawRect.Height > 0 Then task.drawRect = tmpDrawRect
                        BothFirstAndLastReady = False
                    End If

                    If task.pointCloud.Width = 0 Then Continue While Else Exit While
                End If
            End While

            ' camera has exited or resolution is changed.
            If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Or saveWorkingRes <> settings.workingRes Then Exit While
            If frameCount > minFrames Then
                If saveAlgorithmName <> task.algName Then Exit While
                ' switching camera resolution means stopping the current algorithm
                If saveWorkingRes <> settings.workingRes Then Exit While
            End If

            If activeMouseDown = False Then
                SyncLock mouseLock
                    If mousePoint.X < 0 Then mousePoint.X = 0
                    If mousePoint.Y < 0 Then mousePoint.Y = 0
                    If mousePoint.X >= task.workingRes.Width Then mousePoint.X = task.workingRes.Width - 1
                    If mousePoint.Y >= task.workingRes.Height Then mousePoint.Y = task.workingRes.Height - 1

                    task.mouseMovePoint = mousePoint
                    If task.mouseMovePoint = New cv.Point(0, 0) Then
                        task.mouseMovePoint = New cv.Point(task.workingRes.Width / 2, task.workingRes.Height / 2)
                    End If
                    task.mousePicTag = mousePicTag
                    If mouseClickFlag Then
                        task.mouseClickFlag = mouseClickFlag
                        task.clickPoint = mousePoint
                        mouseClickFlag = False
                    End If
                End SyncLock
            End If

            Dim endWaitTime = Now
            Dim elapsedWaitTicks = endWaitTime.Ticks - waitTime.Ticks
            Dim spanWait = New TimeSpan(elapsedWaitTicks)
            task.waitingForInput = spanWait.Ticks / TimeSpan.TicksPerMillisecond - task.inputBufferCopy
            Dim saveDrawRect = task.drawRect

            task.RunAlgorithm() ' <<<<<<<<<<<<<<<<<<<<<<<<< this is where the real work gets done.

            Dim returnTime = Now

            ' in case the algorithm has changed the mouse location...
            If task.mouseMovePointUpdated Then mousePoint = task.mouseMovePoint
            If saveDrawRect <> task.drawRect Then
                drawRect = task.drawRect
                ' relative size of algorithm size image to displayed image
                Dim ratio = camPic(0).Width / task.workingRes.Width
                drawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio,
                                       drawRect.Width * ratio, drawRect.Height * ratio)
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
                    For Each txt In task.flowData
                        trueData.Add(txt)
                    Next
                    task.trueData.Clear()
                    task.flowData.Clear()
                End SyncLock
            End If

            ' when using a recorded video, task.color is the contents of the video.
            ' If task.dst0 was requested, task.color will contain dst0.
            SyncLock paintLock
                dst(0) = task.dst0.Clone
                dst(1) = task.dst1.Clone
                dst(2) = task.dst2.Clone
                dst(3) = task.dst3.Clone
            End SyncLock
            algorithmRefresh = True

            If frameCount Mod task.fpsRate = 0 Then
                SyncLock callTraceLock
                    callTrace = New List(Of String)(task.callTrace)
                    algorithm_ms = New List(Of Single)(task.algorithm_ms)
                    algorithmNames = New List(Of String)(task.algorithmNames)
                End SyncLock
            End If

            Dim elapsedTicks = Now.Ticks - returnTime.Ticks
            Dim span = New TimeSpan(elapsedTicks)
            task.returnCopyTime = span.Ticks / TimeSpan.TicksPerMillisecond

            task.mouseClickFlag = False
            frameCount += 1
            ' this can be very useful.  When debugging your algorithm turned this on to sync output to debug.
            ' Each image will represent the one just finished by the algorithm.
            If task.SyncOutput Then Thread.Sleep(100)
        End While
    End Sub
    Private Sub Options_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
        If TestAllTimer.Enabled Then testAllButton_Click(sender, e)
        Dim saveCameraIndex = settings.cameraIndex

        optionsForm.OptionsDialog_Load(sender, e)
        optionsForm.cameraRadioButton(settings.cameraIndex).Checked = True
        Dim resStr = CStr(settings.workingRes.Width) + "x" + CStr(settings.workingRes.Height)
        For i = 0 To OptionsDialog.resolutionList.Count - 1
            If OptionsDialog.resolutionList(i).StartsWith(resStr) Then
                optionsForm.workingResRadio(i).Checked = True
            End If
        Next

        Dim OKcancel = optionsForm.ShowDialog()

        If OKcancel = DialogResult.OK Then
            If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e)
            restartCameraRequest = True
            saveAlgorithmName = ""
            settings.workingRes = optionsForm.cameraWorkingRes
            settings.displayRes = optionsForm.cameraDisplayRes
            settings.cameraName = optionsForm.cameraName
            settings.cameraIndex = optionsForm.cameraIndex
            settings.testAllDuration = optionsForm.testDuration

            setupCamPics()

            jsonWrite()
            jsonRead() ' this will apply all the changes...

            StartAlgorithmTask()
        Else
            settings.cameraIndex = saveCameraIndex
        End If
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
        Select Case settings.cameraIndex
            Case 0
                Return New CameraKinect(settings.workingRes, settings.captureRes, settings.cameraName)
            Case 1
                Return New CameraRS2(settings.workingRes, settings.captureRes, "Intel RealSense D435I")
            Case 2
                Return New CameraRS2(settings.workingRes, settings.captureRes, "Intel RealSense D455")
            Case 3
                Return Nothing ' special handling required.  See CameraTask...
            Case 4
                Return New CameraZED2(settings.workingRes, settings.captureRes, settings.cameraName)
            Case 5
                Return New CameraMyntD(settings.workingRes, settings.captureRes, settings.cameraName)
        End Select
        Return New CameraKinect(settings.workingRes, settings.captureRes, settings.cameraName)
    End Function
    Private Sub CameraTask()
        restartCameraRequest = True
        For i = 0 To mbuf.Count - 1
            mbuf(i).color = New cv.Mat(settings.workingRes, cv.MatType.CV_8UC3)
            mbuf(i).leftView = New cv.Mat(settings.workingRes, cv.MatType.CV_8UC3)
            mbuf(i).rightView = New cv.Mat(settings.workingRes, cv.MatType.CV_8UC3)
            mbuf(i).pointCloud = New cv.Mat(settings.workingRes, cv.MatType.CV_32FC3)
        Next

        While 1
            If restartCameraRequest Then
                restartCameraRequest = False
                If settings.cameraIndex = 3 Then
                    ' special handling for the Oak-D camera as it cannot be restarted.
                    ' It is my problem but I don't see how to fix it.
                    ' The Oak-D interface cannot run any resolution other than 1280x720 in OpenCVB.
                    ' Changing the working res is not a problem so just leave it open.
                    ' Oak-D camera cannot be restarted without restarting OpenCVB.
                    ' Leave it alone once it is started...
                    settings.captureRes = New cv.Size(1280, 720)
                    camera = New CameraOakD(settings.workingRes, settings.captureRes, settings.cameraName)
                Else
                    If camera IsNot Nothing Then camera.stopCamera()
                    camera = getCamera()
                    newCameraImages = False
                End If
            End If

            camera.GetNextFrame(settings.workingRes)

            SyncLock cameraLock
                mbuf(mbIndex) = camera.mbuf(camera.mbIndex)
                camera.mbindex += 1
                If camera.mbindex >= mbuf.Count Then camera.mbindex = 0
            End SyncLock

            If cameraTaskHandle Is Nothing Then
                camera.stopCamera()
                Exit Sub
            End If

            If camera.mbuf(mbIndex).color.width > 0 Then
                paintNewImages = True ' trigger the paint 
                newCameraImages = True
            End If

            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
            totalBytesOfMemoryUsed = currentProcess.WorkingSet64 / (1024 * 1024)
        End While
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
    Private Sub testAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
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
    Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
        ' don't start another test all algorithm until the current one has finished.
        If algorithmQueueCount <> 0 Then
            Console.WriteLine("Can't start the next algorithm because previous algorithm has not completed.")
            While 1
                If algorithmQueueCount = 0 Then Exit While
                Console.Write(".")
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
            If settings.workingResIndex > testAllEndingRes Then
                While 1
                    settings.cameraIndex += 1
                    settings.cameraName = cameraNames(settings.cameraIndex)
                    If settings.cameraIndex >= cameraNames.Count - 1 Then settings.cameraIndex = 0
                    If settings.cameraPresent(settings.cameraIndex) Then
                        OptionsDialog.defineCameraResolutions(settings.cameraIndex)
                        setupTestAll()
                        settings.workingResIndex = testAllStartingRes
                        Exit While
                    End If
                End While
                ' extra time for the camera to restart...
                TestAllTimer.Interval = settings.testAllDuration * 1000 * 3
            End If

            Select Case settings.workingResIndex
                Case 0
                    settings.workingRes = New cv.Size(1920, 1080)
                    settings.captureRes = New cv.Size(1920, 1080)
                Case 1
                    settings.workingRes = New cv.Size(960, 540)
                    settings.captureRes = New cv.Size(1920, 1080)
                Case 2
                    settings.workingRes = New cv.Size(480, 270)
                    settings.captureRes = New cv.Size(1920, 1080)
                Case 3
                    settings.workingRes = New cv.Size(1280, 720)
                    settings.captureRes = New cv.Size(1280, 720)
                Case 4
                    settings.workingRes = New cv.Size(640, 360)
                    settings.captureRes = New cv.Size(1280, 720)
                Case 5
                    settings.workingRes = New cv.Size(320, 180)
                    settings.captureRes = New cv.Size(1280, 720)
                Case 6
                    settings.workingRes = New cv.Size(640, 480)
                    settings.captureRes = New cv.Size(640, 480)
                Case 7
                    settings.workingRes = New cv.Size(320, 240)
                    settings.captureRes = New cv.Size(640, 480)
                Case 8
                    settings.workingRes = New cv.Size(160, 120)
                    settings.captureRes = New cv.Size(640, 480)
            End Select

            jsonWrite()
            jsonRead()
            LineUpCamPics()

            ' when switching resolution, best to reset these as the move from higher to lower res
            ' could mean the point is no longer valid.
            clickPoint = New cv.Point
            mousePoint = New cv.Point
        End If

        Static saveLastAlgorithm = AvailableAlgorithms.Text
        If saveLastAlgorithm <> AvailableAlgorithms.Text Then
            settings.workingResIndex += 1
            saveLastAlgorithm = AvailableAlgorithms.Text
        End If
        StartAlgorithmTask()
    End Sub
End Class
