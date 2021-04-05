Imports System.ComponentModel
Imports System.Environment
Imports System.Globalization
Imports System.Drawing
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports cv = OpenCvSharp
Imports cvext = OpenCvSharp.Extensions
Imports System.Runtime.InteropServices
Imports System.Management
Module opencv_module
    Public bufferLock As New Mutex(True, "bufferLock") ' this is a global lock on the camera buffers.
    Public delegateLock As New Mutex(True, "delegateLock") ' this is a lock to coordinate paint and the camera task
    Public callTraceLock As New Mutex(True, "callTraceLock")
    Public algorithmThreadLock As New Mutex(True, "AlgorithmThreadLock")
End Module
Public Class OpenCVB
#Region "Globals"
    Dim AlgorithmCount As Integer
    Dim AlgorithmTestCount As Integer
    Dim algorithmTaskHandle As Thread

    Dim saveAlgorithmName As String
    Dim saveCameraName As String
    Dim saveCameraIndex As Integer

    Dim border As Integer = 6
    Dim BothFirstAndLastReady As Boolean

    Dim camera As Object
    Dim cameraRS2Generic As Object ' used only to initialize D435i
    Dim cameraD435i As Object
    Dim cameraD455 As Object
    Dim cameraOakD As Object
    Dim cameraPyRS2 As Object
    Dim cameraKinect As Object
    Dim cameraMyntD As Object
    Dim cameraZed2 As Object
    Dim cameraTaskHandle As Thread
    Dim camPic(3 - 1) As PictureBox

    Dim paintNewImages As Boolean
    Dim taskNewImages As Boolean

    Dim algorithmRefresh As Boolean
    Dim CodeLineCount As Integer
    Dim DrawingRectangle As Boolean
    Dim drawRect As New cv.Rect
    Dim drawRectPic As Integer
    Dim externalPythonInvocation As Boolean
    Dim fps As Integer = 30
    Dim imgResult As New cv.Mat
    Dim frameCount As Integer
    Dim GrabRectangleData As Boolean
    Public HomeDir As DirectoryInfo

    Dim LastX As Integer
    Dim LastY As Integer
    Dim mouseClickFlag As Boolean
    Dim mouseClickPoint As New cv.Point
    Dim mousePicTag As Integer
    Dim mouseDownPoint As New cv.Point
    Dim mouseMovePoint As New cv.Point
    Dim mousePoint As New cv.Point
    Dim ignoreMouseMove As Boolean

    Dim myBrush = New SolidBrush(Color.White)
    Dim openCVKeywords As New List(Of String)
    Public optionsForm As OptionsDialog
    Dim TreeViewDialog As TreeviewForm
    Dim openFileForm As OpenFilename
    Dim picLabels() = {"RGB", "Depth", "", ""}
    Dim resizeForDisplay = 2 ' indicates how much we have to resize to fit on the screen
    Public workingRes As cv.Size
    Dim textDesc As String = ""
    Dim totalBytesOfMemoryUsed As Integer
    Dim ttTextData As List(Of VB_Classes.TTtext)

    Dim openFileDialogRequested As Boolean
    Dim openFileinitialStartSetting As Boolean
    Dim openFileInitialDirectory As String
    Dim openFileFilter As String
    Dim openFileFilterIndex As Integer
    Dim openFileDialogName As String
    Dim openFileStarted As Boolean
    Dim openfileDialogTitle As String
    Dim openfileSliderPercent As Single
    Dim openFileFormLocated As Boolean
    Dim pauseAlgorithmThread As Boolean
    Private Delegate Sub delegateEvent()
    Dim logAlgorithms As StreamWriter
    Public callTrace As New List(Of String)

    Const MAX_RECENT = 25
    Dim recentList As New List(Of String)
    Dim arrowList As New List(Of String)
    Dim recentMenu(MAX_RECENT - 1) As ToolStripMenuItem
    Dim arrowIndex As Integer

    Public intermediateReview As String
    Dim meActivateNeeded As Boolean
    Dim pixelViewerOn As Boolean
    Dim pixelViewerRect As cv.Rect
    Dim pixelViewTag As Integer
    Dim PausePlay As Bitmap
    Dim runPlay As Bitmap
    Dim stopTest As Bitmap
    Dim testAll As Bitmap
    Dim testAllRunning As Boolean
    Dim dropDownActive As Boolean

    Dim surveyActive As Boolean
    Dim nextSurveyImageAvailable As Boolean
    Dim surveyDir As DirectoryInfo
    Dim lastSurveyImage As cv.Mat
#End Region
    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture
        Dim args() = Environment.GetCommandLineArgs()
        ' currently the only commandline arg is the name of the algorithm to run.  Save it and continue...
        If args.Length > 1 Then
            Dim algorithm As String = ""
            SaveSetting("OpenCVB", "OpenCVkeyword", "OpenCVkeyword", "<All>") ' this will guarantee the algorithm is available (if spelled correctly!)
            If args.Length > 2 Then ' arguments from python os.spawnv are passed as wide characters.  
                For i = 0 To args.Length - 1
                    algorithm += args(i)
                Next
            Else
                algorithm = args(1)
            End If
            Console.WriteLine("'" + algorithm + "' was provided in the command line arguments to OpenCVB")
            SaveSetting("OpenCVB", "<All>", "<All>", algorithm)
            externalPythonInvocation = True ' we don't need to start python because it started OpenCVB.
            HomeDir = New DirectoryInfo(CurDir() + "\..\")
        Else
            HomeDir = New DirectoryInfo(CurDir() + "\..\..\")
        End If

        PausePlay = New Bitmap(HomeDir.FullName + "OpenCVB/Data/PauseButton.png")
        stopTest = New Bitmap(HomeDir.FullName + "OpenCVB/Data/StopTest.png")
        testAll = New Bitmap(HomeDir.FullName + "OpenCVB/Data/testall.png")
        runPlay = New Bitmap(HomeDir.FullName + "OpenCVB/Data/PauseButtonRun.png")

        setupRecentList()

        ' Camera DLL's and OpenGL apps are built in Release mode even when configured for Debug (performance is much better).  
        ' OpenGL apps cannot be debugged from OpenCVB and the camera interfaces are not likely to need debugging.
        ' To debug a camera interface: change the Build Configuration and enable "Native Code Debugging" in the OpenCVB project.
        updatePath(HomeDir.FullName + "bin\Release\", "Release Version of camera DLL's.")

        ' check to make sure there are no camera dll's in the Debug directory by mistake!
        For i = 0 To 5
            Dim dllName = Choose(i + 1, "Cam_Kinect4.dll", "Cam_MyntD.dll", "Cam_T265.dll", "Cam_Zed2.dll", "Cam_RS2.dll", "CPP_Classes.dll")
            Dim dllFile = New FileInfo(HomeDir.FullName + "\bin\Debug\" + dllName)
            If dllFile.Exists Then
                ' if the debug dll exists, then remove the Release version because Release is ahead of Debug in the path for this app.
                Dim releaseDLL = New FileInfo(HomeDir.FullName + "\bin\Release\" + dllName)
                If releaseDLL.Exists Then
                    If DateTime.Compare(dllFile.LastWriteTime, releaseDLL.LastWriteTime) > 0 Then releaseDLL.Delete() Else dllFile.Delete()
                End If
            End If
        Next

        Dim DebugDir = HomeDir.FullName + "bin\Debug\"
        updatePath(DebugDir, "Debug Version of any camera DLL's.")

        Dim IntelPERC_Lib_Dir = HomeDir.FullName + "librealsense\build\Debug\"
        updatePath(IntelPERC_Lib_Dir, "Realsense camera support.")
        Dim Kinect_Dir = HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Debug\"
        updatePath(Kinect_Dir, "Kinect camera support.")

        Dim myntSDKready As Boolean
        Dim zed2SDKready As Boolean
        Dim defines = New FileInfo(HomeDir.FullName + "Cameras\CameraDefines.hpp")
        Dim sr = New StreamReader(defines.FullName)
        While sr.EndOfStream = False
            Dim infoLine = Trim(sr.ReadLine)
            If infoLine.StartsWith("//") = False Then
                Dim Split = Regex.Split(infoLine, "\W+")
                If Split(2) = "MYNTD_1000" Then myntSDKready = True
                If Split(2) = "STEREOLAB_INSTALLED" Then zed2SDKready = True
            End If
        End While
        sr.Close()

        openFileForm = New OpenFilename

        optionsForm = New OptionsDialog
        optionsForm.OptionsDialog_Load(sender, e)

        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam) = USBenumeration("Azure Kinect 4K Camera")
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D435i) = USBenumeration("Intel(R) RealSense(TM) Depth Camera 435i Depth")
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D455) = USBenumeration("Intel(R) RealSense(TM) Depth Camera 455  RGB")
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.OakDCamera) = 0 ' USBenumeration("Movidius MyriadX")

        If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D435i) +
                optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D455) > 0 Then
            optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.PythonRS2) = 0 ' Turn RealSense 2 Python interface on and off here...
        End If

        ' Some devices may be present but their opencvb camera interface needs to be present as well.
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.MyntD1000) = USBenumeration("MYNT-EYE-D1000")
        If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.MyntD1000) > 0 And myntSDKready = False Then
            optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.MyntD1000) = 0 ' hardware is there but dll is not installed yet.
            If GetSetting("OpenCVB", "myntSDKready", "myntSDKready", True) Then
                MsgBox("A MYNT D 1000 camera is present but OpenCVB's" + vbCrLf +
                   "Cam_MyntD.dll has not been built with the SDK." + vbCrLf + vbCrLf +
                   "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                   "and rebuild OpenCVB with the MYNT SDK." + vbCrLf + vbCrLf +
                   "Also, add environmental variable " + vbCrLf +
                   "MYNTEYE_DEPTHLIB_OUTPUT" + vbCrLf +
                   "to point to '<MYNT_SDK_DIR>/_output'.")
                SaveSetting("OpenCVB", "myntSDKready", "myntSDKready", False)
            End If
        End If
        optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2) = USBenumeration("ZED 2")
        If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2) > 0 And zed2SDKready = False Then
            optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2) = 0 ' hardware is present but dll is not installed yet.
            If GetSetting("OpenCVB", "zed2SDKready", "zed2SDKready", True) Then
                MsgBox("A StereoLabls ZED 2 camera is present but OpenCVB's" + vbCrLf +
                       "Cam_Zed2.dll has not been built with the SDK." + vbCrLf + vbCrLf +
                       "Edit " + HomeDir.FullName + "CameraDefines.hpp to add support" + vbCrLf +
                       "and rebuild OpenCVB with the StereoLabs SDK.")
                SaveSetting("OpenCVB", "zed2SDKready", "zed2SDKready", False) ' just show this message one time...
            End If
        End If

        checkCameraDefault()

        ' OpenCV needs to be in the path and the librealsense and kinect open source code needs to be in the path.
        updatePath(HomeDir.FullName + "librealsense\build\Release\", "Realsense camera support.")
        updatePath(HomeDir.FullName + "Azure-Kinect-Sensor-SDK\build\bin\Release\", "Kinect camera support.")
        updatePath(HomeDir.FullName + "OpenCV\Build\bin\Debug\", "OpenCV and OpenCV Contrib are needed for C++ classes.")
        updatePath(HomeDir.FullName + "OpenCV\Build\bin\Release\", "OpenCV and OpenCV Contrib are needed for C++ classes.")

        ' the Kinect depthEngine DLL is not included in the SDK.  It is distributed separately because it is NOT open source.
        ' The depthEngine DLL is supposed to be installed in C:\Program Files\Azure Kinect SDK v1.1.0\sdk\windows-desktop\amd64\$(Configuration)
        ' Post an issue if this Is Not a valid assumption
        Dim kinectDLL As New FileInfo("C:\Program Files\Azure Kinect SDK v1.4.1\sdk\windows-desktop\amd64\release\bin\depthengine_2_0.dll")
        If kinectDLL.Exists = False Then
            MsgBox("The Microsoft installer for the Kinect camera proprietary portion" + vbCrLf +
                   "was not installed in:" + vbCrLf + vbCrLf + kinectDLL.FullName + vbCrLf + vbCrLf +
                   "Did a new Version get installed?" + vbCrLf +
                   "Support for the Kinect camera may not work until you update the code near this message.")
            optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam) = 0 ' we can't use this device
        Else
            updatePath(kinectDLL.Directory.FullName, "Kinect depth engine dll.")
        End If

        For i = 0 To VB_Classes.ActiveTask.algParms.camNames.D455
            If optionsForm.cameraDeviceCount(i) > 0 Then optionsForm.cameraTotalCount += 1
        Next

        cameraRS2Generic = New CameraRS2
        Dim RS2count = cameraRS2Generic.queryDeviceCount()
        For i = 0 To RS2count - 1
            Dim deviceName = cameraRS2Generic.queryDevice(i)
            Select Case deviceName
                Case "Intel RealSense D455"
                    cameraD455 = New CameraRS2
                    cameraD455.deviceIndex = i
                    cameraD455.serialNumber = cameraRS2Generic.querySerialNumber(i)
                    cameraD455.cameraName = deviceName
                Case "Intel RealSense D435I"
                    cameraD435i = New CameraRS2
                    cameraD435i.deviceIndex = i
                    cameraD435i.serialNumber = cameraRS2Generic.querySerialNumber(i)
                    cameraD435i.cameraName = deviceName
            End Select
        Next
        cameraKinect = New CameraKinect
        cameraZed2 = New CameraZED2
        cameraMyntD = New CameraMyntD
        cameraOakD = New CameraOakD
        cameraOakD.pythonApp = New FileInfo(HomeDir.FullName + "OpenCVB\CameraOakD.py")
        cameraOakD.pythonexename = optionsForm.PythonExeName.Text

        If cameraD435i IsNot Nothing Or cameraD455 IsNot Nothing Then
            cameraPyRS2 = New CameraPyRS2
            cameraPyRS2.pythonApp = New FileInfo(HomeDir.FullName + "OpenCVB\CameraPyRS2.py")
            cameraPyRS2.pythonexename = optionsForm.PythonExeName.Text
        End If

        startCamera()

        optionsForm.cameraRadioButton(optionsForm.cameraIndex).Checked = True ' make sure any switch is reflected in the UI.
        optionsForm.enableCameras()

        If cameraPyRS2 Is Nothing Then optionsForm.cameraRadioButton(VB_Classes.ActiveTask.algParms.camNames.PythonRS2).Enabled = False

        fpsTimer.Enabled = True
        setupCamPics()
        loadAlgorithmComboBoxes()

        TestAllTimer.Interval = optionsForm.TestAllDuration.Text * 1000
        FindPython()
        If GetSetting("OpenCVB", "TreeButton", "TreeButton", False) Then TreeButton_Click(sender, e)
        If GetSetting("OpenCVB", "PixelViewerActive", "PixelViewerActive", False) Then PixelViewerButton_Click(sender, e)
    End Sub
    Private Sub campic_Paint(sender As Object, e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        Dim pic = DirectCast(sender, PictureBox)
        Dim ratio = camPic(2).Width / imgResult.Width
        g.ScaleTransform(1, 1)
        g.DrawImage(pic.Image, 0, 0)

        Static myWhitePen As New Pen(Color.White)
        Static myBlackPen As New Pen(Color.Black)

        If surveyActive And frameCount > 10 Then
            If pic.Tag = 2 And nextSurveyImageAvailable = False Then
                SyncLock imgResult
                    lastSurveyImage = imgResult.Clone()
                    SurveyTimer.Enabled = True
                    nextSurveyImageAvailable = True
                End SyncLock
            End If
        End If
        If pixelViewerOn And (mousePicTag = pic.Tag Or (pixelViewTag = 3 And pic.Tag = 2)) Then
            Dim pic3Offset As Integer
            If pixelViewTag = 3 Then pic3Offset = imgResult.Width / 2
            Dim r = pixelViewerRect
            Dim rect = New cv.Rect(CInt((r.X + pic3Offset) * ratio), CInt(r.Y * ratio), CInt(r.Width * ratio), CInt(r.Height * ratio))
            g.DrawRectangle(myWhitePen, rect.X, rect.Y, rect.Width, rect.Height)
            g.DrawRectangle(myBlackPen, Math.Max(0, rect.X - 1), Math.Max(0, rect.Y - 1), Math.Min(rect.Width + 2, camPic(0).Width - 1), Math.Min(rect.Height + 2, camPic(0).Height - 1))
        End If

        If drawRect.Width > 0 And drawRect.Height > 0 Then
            g.DrawRectangle(myWhitePen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height)
            If pic.Tag = 2 Then
                g.DrawRectangle(myWhitePen, drawRect.X + camPic(0).Width, drawRect.Y, drawRect.Width, drawRect.Height)
            End If
        End If

        If algorithmRefresh And (pic.Tag = 2) Then
            algorithmRefresh = False
            SyncLock imgResult
                Try
                    Dim result = imgResult.Resize(New cv.Size(camPic(2).Size.Width, camPic(2).Size.Height))
                    cvext.BitmapConverter.ToBitmap(result, camPic(2).Image)
                Catch ex As Exception
                    Console.WriteLine("OpenCVB: Error in OpenCVB/Paint updating dst output: " + ex.Message)
                End Try
            End SyncLock
        End If

        If paintNewImages And (pic.Tag = 0 Or pic.Tag = 1) Then
            paintNewImages = False
            If camera.color IsNot Nothing Then
                If camera.color.width > 0 Then
                    SyncLock delegateLock
                        Dim RGBDepth = camera.RGBDepth.Resize(New cv.Size(camPic(1).Size.Width, camPic(1).Size.Height))
                        Dim color = camera.color.Resize(New cv.Size(camPic(0).Size.Width, camPic(0).Size.Height))
                        Try
                            cvext.BitmapConverter.ToBitmap(color, camPic(0).Image)
                            cvext.BitmapConverter.ToBitmap(RGBDepth, camPic(1).Image)
                        Catch ex As Exception
                            Console.WriteLine("OpenCVB: Error in campic_Paint: " + ex.Message)
                        End Try
                    End SyncLock
                End If
            End If
        End If

        ' draw any TrueType font data on the image 
        Dim maxline = 21
        SyncLock ttTextData
            Try
                If pic.Tag = 2 Or pic.Tag = 3 Then
                    Dim ttText = New List(Of VB_Classes.TTtext)(ttTextData)
                    For i = 0 To ttText.Count - 1
                        Dim tt = ttText(i)
                        If tt IsNot Nothing Then
                            If ttText(i).picTag = 3 Then
                                g.DrawString(tt.text, optionsForm.fontInfo.Font, New SolidBrush(Color.White),
                                             tt.x * ratio + camPic(0).Width, tt.y * ratio)
                            Else
                                g.DrawString(tt.text, optionsForm.fontInfo.Font, New SolidBrush(Color.White),
                                             tt.x * ratio, tt.y * ratio)
                            End If
                            maxline -= 1
                            If maxline <= 0 Then Exit For
                        End If
                    Next
                End If
            Catch ex As Exception
                Console.WriteLine("Error in ttextData update: " + ex.Message)
            End Try

            If optionsForm.ShowLabels.Checked Then
                Dim textRect As New Rectangle(0, 0, camPic(0).Width / 2, If(resizeForDisplay = 4, 12, 20))
                If Len(picLabels(pic.Tag)) Then g.FillRectangle(myBrush, textRect)
                g.DrawString(picLabels(pic.Tag), optionsForm.fontInfo.Font, New SolidBrush(Color.Black), 0, 0)
                If Len(picLabels(3)) Then
                    textRect = New Rectangle(camPic(0).Width, 0, camPic(0).Width / 2, If(resizeForDisplay = 4, 12, 20))
                    g.FillRectangle(myBrush, textRect)
                    g.DrawString(picLabels(3), optionsForm.fontInfo.Font, New SolidBrush(Color.Black), camPic(0).Width, 0)
                End If
            End If
        End SyncLock

        ' only the main task can have an openfiledialog box!  Move results to the algorithm task from specified locations in this form.
        If openFileInitialDirectory <> "" Then
            If openFileDialogRequested Then
                openFileDialogRequested = False
                openFileForm.OpenFileDialog1.InitialDirectory = openFileInitialDirectory
                openFileForm.OpenFileDialog1.FileName = "*.*"
                openFileForm.OpenFileDialog1.CheckFileExists = False
                openFileForm.OpenFileDialog1.Filter = openFileFilter
                openFileForm.OpenFileDialog1.FilterIndex = openFileFilterIndex
                openFileForm.filename.Text = openFileDialogName
                openFileForm.Text = openfileDialogTitle
                openFileForm.Label1.Text = "Select a file for use with the " + AvailableAlgorithms.Text + " algorithm."
                openFileForm.Show()
                openFileStarted = openFileinitialStartSetting
                If openFileinitialStartSetting And openFileForm.PlayButton.Text = "Start" Then
                    openFileForm.PlayButton.PerformClick()
                Else
                    If openFileinitialStartSetting = False Then
                        openFileForm.fileStarted = False
                        openFileForm.PlayButton.Text = "Start"
                    End If
                End If
            Else
                If (openFileForm.Location.X <> Me.Left Or openFileForm.Location.Y <> Me.Top + Me.Height) And openFileFormLocated = False Then
                    openFileFormLocated = True
                    openFileForm.Location = New Point(Me.Left, Me.Top + Me.Height)
                End If
                If openFileDialogName <> openFileForm.filename.Text Then openFileDialogName = openFileForm.filename.Text
                If openfileSliderPercent >= 0 And openfileSliderPercent <= 1 Then openFileForm.TrackBar1.Value = openfileSliderPercent * 10000
                openFileForm.PlayButton.Visible = openfileSliderPercent >= 0 ' negative indicates it should not be shown.
                openFileForm.TrackBar1.Visible = openFileForm.PlayButton.Visible
            End If
            openFileStarted = openFileForm.fileStarted
        End If
        AlgorithmDesc.Text = textDesc
    End Sub
    Private Sub checkCameraDefault()
        ' if the default camera is not present, try to find another.
        If optionsForm.cameraDeviceCount(optionsForm.cameraIndex) = 0 Then
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam) Then
                optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.Kinect4AzureCam
            End If
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.StereoLabsZED2
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.MyntD1000) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.MyntD1000
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D435i) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.D435i
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.D455) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.D455
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.OakDCamera) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.OakDCamera
            If optionsForm.cameraDeviceCount(VB_Classes.ActiveTask.algParms.camNames.PythonRS2) Then optionsForm.cameraIndex = VB_Classes.ActiveTask.algParms.camNames.PythonRS2
            If optionsForm.cameraDeviceCount(optionsForm.cameraIndex) = 0 Then
                MsgBox("There are no supported cameras present!" + vbCrLf + vbCrLf +
                       "Connect any of these cameras: " + vbCrLf + vbCrLf + "Intel RealSense2 D455" + vbCrLf + "Intel RealSense2 D435i" + vbCrLf +
                       "OpenCV Oak-D camera" + vbCrLf + "Microsoft Kinect 4 Azure" + vbCrLf + "MyntEyeD 1000" + vbCrLf + "StereoLabs Zed2")
            End If
        End If
    End Sub
    Private Sub jumpToAlgorithm(algName As String)
        If AvailableAlgorithms.Items.Contains(algName) = False Then
            AvailableAlgorithms.SelectedIndex = 0
        Else
            AvailableAlgorithms.SelectedItem = algName
        End If
    End Sub
    Private Sub recentList_Clicked(sender As Object, e As EventArgs)
        arrowIndex = 0
        Dim item = TryCast(sender, ToolStripMenuItem)
        jumpToAlgorithm(item.Name)
    End Sub
    Private Sub ToolStripButton1_Click(sender As Object, e As EventArgs) Handles ToolStripButton1.Click
        If arrowIndex = 0 Then
            arrowList.Clear()
            For i = 0 To recentList.Count - 1
                arrowList.Add(recentList.ElementAt(i))
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
    Private Sub setupRecentList()
        For i = 0 To MAX_RECENT - 1
            Dim nextA = GetSetting("OpenCVB", "RecentList" + CStr(i), "RecentList" + CStr(i), "recent algorithm " + CStr(i))
            If nextA = "" Then Exit For
            If recentList.Contains(nextA) = False Then
                recentList.Add(nextA)
                recentMenu(i) = New ToolStripMenuItem() With {.Text = nextA, .Name = nextA}
                AddHandler recentMenu(i).Click, AddressOf recentList_Clicked
                MainMenu.DropDownItems.Add(recentMenu(i))
            End If
        Next
    End Sub
    Private Sub updateRecentList()
        If TestAllTimer.Enabled Then Exit Sub
        Dim copyList As List(Of String)
        If recentList.Contains(AvailableAlgorithms.Text) Then
            ' make it the most recent
            copyList = New List(Of String)(recentList)
            recentList.Clear()
            recentList.Add(AvailableAlgorithms.Text)
            For i = 0 To copyList.Count - 1
                If recentList.Contains(copyList(i)) = False Then recentList.Add(copyList(i))
            Next
        Else
            recentList.RemoveAt(recentList.Count - 1)
            copyList = New List(Of String)(recentList)
            recentList.Clear()
            recentList.Add(AvailableAlgorithms.Text)
            For i = 0 To copyList.Count - 1
                If recentList.Contains(copyList(i)) = False Then recentList.Add(copyList(i))
            Next
        End If
        For i = 0 To recentList.Count - 1
            If recentList(i) <> "" Then
                If recentMenu(i) Is Nothing Then
                    recentMenu(i) = New ToolStripMenuItem() With {.Text = recentList(i), .Name = recentList(i)}
                    AddHandler recentMenu(i).Click, AddressOf recentList_Clicked
                End If
                recentMenu(i).Text = recentList(i)
                recentMenu(i).Name = recentList(i)
                SaveSetting("OpenCVB", "RecentList" + CStr(i), "RecentList" + CStr(i), recentList(i))
            End If
        Next
    End Sub
    Private Sub testAllButton_Click(sender As Object, e As EventArgs) Handles TestAllButton.Click
        TestAllButton.Image = If(TestAllButton.Text = "Test All", stopTest, testAll)

        If TestAllButton.Text = "Test All" Then
            TestAllButton.Text = "Stop Test"
            TestAllTimer_Tick(sender, e)
            TestAllTimer.Enabled = True
            If TreeViewDialog IsNot Nothing Then TreeViewDialog.Timer1.Enabled = True
        Else
            TestAllTimer.Enabled = False
            TestAllButton.Text = "Test All"
            StartAlgorithmTask()
        End If
    End Sub
    Private Sub TestAllTimer_Tick(sender As Object, e As EventArgs) Handles TestAllTimer.Tick
        If frameCount = 0 And TestAllButton.Text = "Stop Test" Then Exit Sub ' we have to see some output from the algorithm before moving on...
        If AlgorithmTestCount Mod AvailableAlgorithms.Items.Count = 0 And AlgorithmTestCount > 0 Then
            'If optionsForm.resolution640.Enabled And optionsForm.resolution1280.Checked Then
            '    optionsForm.resolution640.Checked = True
            '    LineUpCamPics(False)
            '    startCamera()
            'Else
            optionsForm.resolution1280.Checked = True ' start every camera at 1280x720
            Dim cameraIndex = optionsForm.cameraIndex + 1
            For i = 0 To optionsForm.cameraRadioButton.Count - 1
                If cameraIndex >= optionsForm.cameraRadioButton.Count Then cameraIndex = 0
                If optionsForm.cameraRadioButton(cameraIndex).Enabled Then
                    optionsForm.cameraRadioButton(cameraIndex).Checked = True
                    optionsForm.cameraIndex = cameraIndex
                    LineUpCamPics(False)
                    startCamera()
                    Exit For
                Else
                    cameraIndex += 1
                End If
            Next
            ' End If
        End If

        If AvailableAlgorithms.SelectedIndex < AvailableAlgorithms.Items.Count - 1 Then
            AvailableAlgorithms.SelectedIndex += 1
        Else
            If AvailableAlgorithms.Items.Count = 1 Then ' selection index won't change if there is only one algorithm in the list.
                StartAlgorithmTask()
            Else
                AvailableAlgorithms.SelectedIndex = 0
            End If
        End If
    End Sub
    Private Sub startCamera()
        SyncLock bufferLock
            saveCameraIndex = optionsForm.cameraIndex
        End SyncLock
        If saveAlgorithmName IsNot Nothing Then StartAlgorithmTask() ' restart the currealgorithm...
        paintNewImages = False
        taskNewImages = False
        If cameraTaskHandle Is Nothing Then
            cameraTaskHandle = New Thread(AddressOf CameraTask)
            cameraTaskHandle.Start()
        End If

        While paintNewImages = False
            Application.DoEvents() ' wait for the first images from the camera.
        End While
        SaveSetting("OpenCVB", "CameraIndex", "CameraIndex", optionsForm.cameraIndex)
    End Sub
    Private Sub CameraTask()
        On Error Resume Next
        Dim currentCameraIndex = -1
        While (1)
            SyncLock bufferLock
                If currentCameraIndex <> saveCameraIndex Then
                    saveAlgorithmName = "" ' this will restart the current algorithm
                    If saveCameraIndex < 0 Then Exit While
                    If camera IsNot Nothing Then camera.stopCamera()
                    ' order is same as in optionsdialog enum
                    camera = Choose(saveCameraIndex + 1, cameraKinect, cameraZed2, cameraMyntD, cameraD435i, cameraD455, cameraPyRS2, cameraOakD)
                    currentCameraIndex = saveCameraIndex
                    camera.initialize(workingRes.Width, workingRes.Height, fps)
                    saveCameraName = camera.deviceName + " " + CStr(workingRes.Width)
                End If
                camera.GetNextFrame()
            End SyncLock

            ' make sure that all the images have something...
            If frameCount = 0 Then
                For i = 0 To 5
                    Dim mat = Choose(i + 1, camera.color, camera.rgbdepth, camera.depth16, camera.leftview, camera.rightview, camera.pointcloud)
                    If mat.size <> workingRes Then Continue For ' try again...
                Next
            End If

            paintNewImages = True ' trigger the paint 
            taskNewImages = True ' trigger the algorithm task

            Static delegateX As New delegateEvent(AddressOf raiseEventCamera)
            Me.Invoke(delegateX)

            GC.Collect() ' minimize memory footprint - the frames have just been sent so this task isn't busy.

            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
            totalBytesOfMemoryUsed = currentProcess.WorkingSet64 / (1024 * 1024)
        End While
    End Sub
    Private Sub TreeButton_Click(sender As Object, e As EventArgs) Handles TreeButton.Click
        TreeButton.Checked = Not TreeButton.Checked
        If TreeButton.Checked Then
            TreeViewDialog = New TreeviewForm
            TreeViewDialog.updateTree()
            TreeViewDialog.TreeviewForm_Resize(sender, e)
            TreeViewDialog.Show()
            TreeViewDialog.BringToFront()
        Else
            TreeViewDialog.Close()
        End If
    End Sub
    Private Sub PixelViewerButton_Click(sender As Object, e As EventArgs) Handles PixelViewerButton.Click
        PixelViewerButton.Checked = Not PixelViewerButton.Checked
        pixelViewerOn = PixelViewerButton.Checked
    End Sub
    Public Function USBenumeration(searchName As String) As Integer
        Static firstCall = 0
        Dim deviceCount As Integer
        ' See if the desired device shows up in the device manager.'
        Dim info As ManagementObject
        Dim search As ManagementObjectSearcher
        Dim Name As String
        search = New ManagementObjectSearcher("SELECT * From Win32_PnPEntity")
        For Each info In search.Get()
            Name = CType(info("Caption"), String) ' Get the name of the device.'
            ' toss the uninteresting names so we can find the cameras.
            If Name Is Nothing Then Continue For
            If firstCall = 0 Then
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
                    Name.StartsWith("PCI-to-PCI") Or Name.StartsWith("Network Controller") Or Name.StartsWith("ATAPI ") Then
                Else
                    Console.WriteLine(Name) ' looking for new cameras 
                End If
            End If
            If InStr(Name, searchName, CompareMethod.Text) > 0 Then
                If firstCall = 0 Then Console.WriteLine(Name)
                deviceCount += 1
            End If
        Next
        firstCall += 1
        Return deviceCount
    End Function
    Private Sub setupCamPics()
        Me.Left = GetSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        Me.Top = GetSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)

        Dim goodPoint = Screen.GetWorkingArea(New Point(Me.Left, Me.Top)) ' when they change the main screen, old coordinates can go way off the screen.
        If goodPoint.X > Me.Left Then Me.Left = goodPoint.X
        If goodPoint.Y > Me.Top Then Me.Top = goodPoint.Y

        Dim defaultWidth = workingRes.Width * 2 + border * 7
        Dim defaultHeight = workingRes.Height * 2 + ToolStrip1.Height + border * 12
        Me.Width = GetSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", defaultWidth)
        Me.Height = GetSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", defaultHeight)
        If Me.Height < 50 Then
            Me.Width = defaultWidth
            Me.Height = defaultHeight
        End If

        ttTextData = New List(Of VB_Classes.TTtext)

        For i = 0 To camPic.Length - 1
            If camPic(i) Is Nothing Then camPic(i) = New PictureBox()
            camPic(i).Size = New Size(If(i < 2, workingRes.Width, workingRes.Width * 2), workingRes.Height)
            AddHandler camPic(i).DoubleClick, AddressOf campic_DoubleClick
            AddHandler camPic(i).Click, AddressOf campic_Click
            AddHandler camPic(i).Paint, AddressOf campic_Paint
            AddHandler camPic(i).MouseDown, AddressOf camPic_MouseDown
            AddHandler camPic(i).MouseUp, AddressOf camPic_MouseUp
            AddHandler camPic(i).MouseMove, AddressOf camPic_MouseMove
            camPic(i).Tag = i
            Me.Controls.Add(camPic(i))
        Next
        LineUpCamPics(resizing:=False)
    End Sub
    Private Sub OpenCVB_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        If camPic Is Nothing Then Exit Sub ' when first opening, campic may not be built yet
        If camPic(2) Is Nothing Then Exit Sub ' individual pictureboxes need to be ready as well.
        LineUpCamPics(resizing:=True)
    End Sub
    Private Sub LineUpCamPics(resizing As Boolean)
        If resizing = False Then
            If optionsForm.SnapToGrid.Checked Then
                Select Case workingRes.Height
                    Case 240
                        Me.Width = 683
                        Me.Height = 592
                    Case 480
                        Me.Width = 1321
                        Me.Height = 1071
                    Case 720
                        Me.Width = 1321
                        Me.Height = 835
                End Select
            End If
        End If

        Dim width = CInt((Me.Width - 42) / 2)
        Dim height = CInt(width * workingRes.Height / workingRes.Width)
        If Math.Abs(width - workingRes.Width / 2) < 2 Then width = workingRes.Width / 2
        If Math.Abs(height - workingRes.Height / 2) < 2 Then height = workingRes.Height / 2
        Dim padX = 12
        Dim padY = 60
        camPic(0).Size = New Size(width, height)
        camPic(1).Size = New Size(width, height)
        camPic(2).Size = New Size(width * 2, height)

        camPic(0).Image = New Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(1).Image = New Bitmap(width, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(2).Image = New Bitmap(width * 2, height, Imaging.PixelFormat.Format24bppRgb)
        camPic(0).Location = New Point(padX, padY)
        camPic(1).Location = New Point(camPic(0).Left + camPic(0).Width, padY)
        camPic(2).Location = New Point(padX, camPic(0).Top + camPic(0).Height)
        saveLayout()
    End Sub
    Private Sub FindPython()
        Dim pythonStr = GetSetting("OpenCVB", "PythonExe", "PythonExe", "")
        If pythonStr = "" Then pythonStr = GetFolderPath(SpecialFolder.ApplicationData)
        Dim currentName = New FileInfo(pythonStr)
        If currentName.Exists = False Then
            Dim appData = GetFolderPath(SpecialFolder.ApplicationData)
            Dim directoryInfo As New DirectoryInfo(appData + "\..\Local\Programs\Python\")
            If directoryInfo.Exists = False Then
                ' let's try to find python.exe in appData\Local\Microsoft\WindowsApp
                Dim pyFile = New FileInfo(appData + "\..\Local\Microsoft\WindowsApps\python.exe")
                If pyFile.Exists = False Then
                    MsgBox("OpenCVB cannot find an active Python." + vbCrLf + "Use OpenCVB's Global Settings to specify Python.exe.")
                    Exit Sub
                Else
                    SaveSetting("OpenCVB", "PythonExe", "PythonExe", pyFile.FullName)
                    Exit Sub
                End If
            End If
            For Each Dir As String In System.IO.Directory.GetDirectories(directoryInfo.FullName)
                Dim dirInfo As New System.IO.DirectoryInfo(Dir)
                Dim pythonFileInfo = New FileInfo(dirInfo.FullName + "\Python.exe")
                If pythonFileInfo.Exists Then
                    SaveSetting("OpenCVB", "PythonExe", "PythonExe", pythonFileInfo.FullName)
                    optionsForm.PythonExeName.Text = pythonFileInfo.FullName
                    Exit For
                End If
            Next
        End If
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
            MsgBox("The AlgorithmMapToOpenCV.txt file is missing.  Look at Index Project that creates the mapping of algorithms to OpenCV keywords.")
            End
        End If
        sr = New StreamReader(AlgorithmMapFileInfo.FullName)
        OpenCVkeyword.Items.Clear()
        While sr.EndOfStream = False
            infoLine = sr.ReadLine
            openCVKeywords.Add(infoLine)
            Split = Regex.Split(infoLine, ",")
            OpenCVkeyword.Items.Add(Split(0))
        End While
        sr.Close()

        OpenCVkeyword.Text = GetSetting("OpenCVB", "OpenCVkeyword", "OpenCVkeyword", "<All>")
        If OpenCVkeyword.Text = "" Then OpenCVkeyword.Text = "<All>"
        SaveSetting("OpenCVB", "OpenCVkeyword", "OpenCVkeyword", OpenCVkeyword.Text)
    End Sub
    Private Sub OpenCVkeyword_SelectedIndexChanged(sender As Object, e As EventArgs) Handles OpenCVkeyword.SelectedIndexChanged
        If OpenCVkeyword.Text = "<All>" Or OpenCVkeyword.Text = "<All using recorded data>" Then
            Dim AlgorithmListFileInfo = New FileInfo(HomeDir.FullName + "Data/AlgorithmList.txt")
            Dim sr = New StreamReader(AlgorithmListFileInfo.FullName)

            Dim infoLine = sr.ReadLine
            Dim Split = Regex.Split(infoLine, "\W+")
            CodeLineCount = Split(1)
            AvailableAlgorithms.Items.Clear()
            While sr.EndOfStream = False
                infoLine = sr.ReadLine
                infoLine = UCase(Mid(infoLine, 1, 1)) + Mid(infoLine, 2)
                AvailableAlgorithms.Items.Add(infoLine)
            End While
            sr.Close()
        Else
            AvailableAlgorithms.Enabled = False
            Dim keyIndex = OpenCVkeyword.Items.IndexOf(OpenCVkeyword.Text)
            Dim openCVkeys = openCVKeywords(keyIndex)
            Dim split = Regex.Split(openCVkeys, ",")
            AvailableAlgorithms.Items.Clear()
            For i = 1 To split.Length - 1
                AvailableAlgorithms.Items.Add(split(i))
            Next
            AvailableAlgorithms.Enabled = True
        End If

        AvailableAlgorithms.Text = GetSetting("OpenCVB", OpenCVkeyword.Text, OpenCVkeyword.Text, AvailableAlgorithms.Items(0))

        Dim index = AvailableAlgorithms.Items.IndexOf(AvailableAlgorithms.Text)
        If index < 0 Then AvailableAlgorithms.SelectedIndex = 0 Else AvailableAlgorithms.SelectedIndex = index
        SaveSetting("OpenCVB", "OpenCVkeyword", "OpenCVkeyword", OpenCVkeyword.Text)
    End Sub
    Private Sub AvailableAlgorithms_SelectedIndexChanged(sender As Object, e As EventArgs) Handles AvailableAlgorithms.SelectedIndexChanged
        If AvailableAlgorithms.Enabled Then
            If PausePlayButton.Text = "Run" Then PausePlayButton_Click(sender, e) ' if paused, then restart.
            If OpenCVkeyword.Text = "" Then Exit Sub ' we are terminating...
            SaveSetting("OpenCVB", OpenCVkeyword.Text, OpenCVkeyword.Text, AvailableAlgorithms.Text)
            StartAlgorithmTask()
            updateRecentList()
        End If
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
        Dim ratio = imgResult.Width / camPic(2).Width
        r = New cv.Rect(r.X * ratio, r.Y * ratio, r.Width * ratio, r.Height * ratio)
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
        mouseClickFlag = True
    End Sub
    Private Sub camPic_MouseUp(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            If DrawingRectangle Then
                DrawingRectangle = False
                GrabRectangleData = True
            End If
            ignoremousemove = False
        Catch ex As Exception
            Console.WriteLine("Error in camPic_MouseUp: " + ex.Message)
        End Try
    End Sub

    Private Sub camPic_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            Dim pic = DirectCast(sender, PictureBox)
            If e.Button = Windows.Forms.MouseButtons.Right Then
                ignoreMouseMove = True
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
    Private Sub AvailableAlgorithms_MouseClick(sender As Object, e As MouseEventArgs) Handles AvailableAlgorithms.MouseClick
        ' If they Then had been Using the treeview feature To click On a tree entry, the timer was disable.  
        ' Clicking on availablealgorithms indicates they are done with using the treeview.
        If TreeViewDialog IsNot Nothing Then TreeViewDialog.Timer1.Enabled = True
    End Sub
    Private Sub camPic_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs)
        Try
            Dim pic = DirectCast(sender, PictureBox)
            If ignoreMouseMove Then Exit Sub
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
                    drawRectPic = 3 ' When wider than campic(0), it can only be dst2 which has no pic.tag (because campic(2) is double-wide for timing reasons.
                End If
                drawRect.Y = Math.Min(mouseDownPoint.Y, mouseMovePoint.Y)
                drawRect.Width = Math.Abs(mouseDownPoint.X - mouseMovePoint.X)
                drawRect.Height = Math.Abs(mouseDownPoint.Y - mouseMovePoint.Y)
                If drawRect.X + drawRect.Width > workingRes.Width Then drawRect.Width = workingRes.Width - drawRect.X
                If drawRect.Y + drawRect.Height > workingRes.Height Then drawRect.Height = workingRes.Height - drawRect.Y
                BothFirstAndLastReady = True
            End If
            mousePicTag = pic.Tag
            mousePoint.X = e.X
            mousePoint.Y = e.Y
            If mousePicTag = 2 And mousePoint.X > camPic(0).Width Then
                mousePoint.X -= camPic(0).Width
                mousePicTag = 3 ' pretend this is coming from the fictional campic(3) which was dst2
            End If
            mousePoint *= workingRes.Width / camPic(0).Width

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
            PausePlayButton.Image = PausePlay
        End If
    End Sub
    Private Sub OpenCVB_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd
        saveLayout()
    End Sub
    Public Sub raiseEventCamera()
        SyncLock delegateLock
            For i = 0 To camPic.Length - 1
                camPic(i).Refresh()
            Next
        End SyncLock
    End Sub
    Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
        Static lastAlgorithmFrame As Integer
        Static lastCameraFrame As Integer
        If lastAlgorithmFrame > frameCount Then lastAlgorithmFrame = 0
        If lastCameraFrame > camera.frameCount Then lastCameraFrame = 0
        If AvailableAlgorithms.Text.Contains(".py") Then meActivateNeeded = False
        If AvailableAlgorithms.Text.StartsWith("OpenGL") Then meActivateNeeded = False
        If meActivateNeeded Then
            Me.Activate()
            meActivateNeeded = False
        End If
        If TreeViewDialog IsNot Nothing Then
            If TreeViewDialog.TreeView1.IsDisposed Then TreeButton.CheckState = CheckState.Unchecked
        End If

        Dim countFrames = frameCount - lastAlgorithmFrame
        lastAlgorithmFrame = frameCount
        Dim algorithmFPS As Single = countFrames / (fpsTimer.Interval / 1000)

        Dim camFrames = camera.frameCount - lastCameraFrame
        lastCameraFrame = camera.frameCount
        Dim cameraFPS As Single = camFrames / (fpsTimer.Interval / 1000)

        Me.Text = "OpenCVB (" + Format(CodeLineCount, "###,##0") + " lines / " + CStr(AlgorithmCount) + " algorithms = " + CStr(CInt(CodeLineCount / AlgorithmCount)) +
                  " lines per) - " + optionsForm.cameraRadioButton(saveCameraIndex).Text + " - " + Format(cameraFPS, "#0.0") +
                  "/" + Format(algorithmFPS, "#0.0") + " " + CStr(totalBytesOfMemoryUsed) + " Mb (working set) with " +
                  CStr(Process.GetCurrentProcess().Threads.Count) + " threads"
    End Sub
    Private Sub saveLayout()
        optionsForm.saveResolution()
        SaveSetting("OpenCVB", "OpenCVBLeft", "OpenCVBLeft", Me.Left)
        SaveSetting("OpenCVB", "OpenCVBTop", "OpenCVBTop", Me.Top)
        SaveSetting("OpenCVB", "OpenCVBWidth", "OpenCVBWidth", Me.Width)
        SaveSetting("OpenCVB", "OpenCVBHeight", "OpenCVBHeight", Me.Height)

        Dim resolutionDesc As String = ""
        Select Case optionsForm.resolutionName
            Case "Low"
                resolutionDesc = "320x240"
            Case "Medium"
                resolutionDesc = "640x480"
            Case "High"
                resolutionDesc = "1280x720"
        End Select
        Dim details = " Display at " + CStr(camPic(0).Width) + "x" + CStr(camPic(0).Height) + ", Working Res. = " + resolutionDesc
        picLabels(0) = "RGB:" + details
        picLabels(1) = "Depth:" + details
    End Sub
    Private Sub Exit_Click(sender As Object, e As EventArgs) Handles ExitCall.Click
        SaveSetting("OpenCVB", "TreeButton", "TreeButton", TreeButton.Checked)
        SaveSetting("OpenCVB", "PixelViewerActive", "PixelViewerActive", PixelViewerButton.Checked)
        saveCameraIndex = -1 ' this will close the camera task
        saveAlgorithmName = "" ' this will close the current algorithm.
        saveLayout()
    End Sub
    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("The objective is to solve many small computer vision problems " + vbCrLf +
               "and do so in a way that enables any of the solutions to be reused." + vbCrLf +
               "The result is a toolkit for solving ever bigger and more difficult" + vbCrLf +
               "problems.  The philosophy behind this approach is that human vision" + vbCrLf +
               "is not computationally intensive but is built on many almost trivial" + vbCrLf +
               "algorithms working together." + vbCrLf)
    End Sub
    Private Sub MainFrm_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        Exit_Click(sender, e)
    End Sub
    Private Sub SnapShotButton_Click(sender As Object, e As EventArgs) Handles SnapShotButton.Click
        Dim img As New Bitmap(Me.Width, Me.Height)
        Me.DrawToBitmap(img, New Rectangle(0, 0, Me.Width, Me.Height))

        Dim snapForm = New SnapshotRequest
        snapForm.SnapshotRequest_Load(sender, e)
        snapForm.PictureBox1.Image = img
        snapForm.AllImages.Checked = True
        If snapForm.ShowDialog() <> DialogResult.OK Then Exit Sub

        Dim resultMat As New cv.Mat
        For i = 0 To 4
            Dim radioButton = Choose(i + 1, snapForm.AllImages, snapForm.ColorImage, snapForm.RGBDepth, snapForm.Result1, snapForm.Result2)
            If radioButton.checked Then
                SyncLock bufferLock
                    Select Case i
                        Case 0 ' all images
                            resultMat = cv.Extensions.BitmapConverter.ToMat(img)
                        Case 1 ' color image
                            resultMat = camera.Color.Clone()
                        Case 2 ' depth RGB
                            resultMat = camera.RGBDepth.Clone()
                        Case 3 ' result1
                            resultMat = imgResult(New cv.Rect(0, 0, imgResult.Width / 2, imgResult.Height)).Clone()
                        Case 4 ' result2
                            resultMat = imgResult(New cv.Rect(imgResult.Width / 2, 0, imgResult.Width / 2, imgResult.Height)).Clone()
                    End Select
                    Exit For
                End SyncLock
            End If
        Next
        img = cv.Extensions.BitmapConverter.ToBitmap(resultMat)
        Clipboard.SetImage(img)
    End Sub
    Private Sub Options_Click(sender As Object, e As EventArgs) Handles OptionsButton.Click
        If TestAllTimer.Enabled Then testAllButton_Click(sender, e)

        saveAlgorithmName = ""
        Dim OKcancel = optionsForm.ShowDialog()

        If OKcancel = DialogResult.OK Then
            optionsForm.saveResolution()
            If saveCameraIndex <> optionsForm.cameraIndex Or camera.width <> workingRes.Width Then startCamera()
            TestAllTimer.Interval = optionsForm.TestAllDuration.Value * 1000

            LineUpCamPics(resizing:=False)
            saveLayout()
        End If
        StartAlgorithmTask()
    End Sub
    Private Sub StartAlgorithmTask()
        Console.WriteLine("Queuing up: " + AvailableAlgorithms.Text)
        saveAlgorithmName = AvailableAlgorithms.Text ' this tells the previous algorithmTask to terminate.
        openFileForm.Hide()
        openFileForm.PlayButton.Text = "Start"
        openFileDialogName = ""
        openFileInitialDirectory = ""
        openFileForm.fileStarted = False
        openFileFormLocated = False

        Dim parms As New VB_Classes.ActiveTask.algParms
        ReDim parms.RotationMatrix(9 - 1)
        parms.RotationVector = camera.RotationVector
        parms.IMU_Present = True ' always present!

        parms.cameraName = GetSetting("OpenCVB", "CameraIndex", "CameraIndex", VB_Classes.ActiveTask.algParms.camNames.D435i)
        parms.PythonExe = optionsForm.PythonExeName.Text

        parms.useRecordedData = OpenCVkeyword.Text = "<All using recorded data>"
        testAllRunning = TestAllButton.Text = "Stop Test"
        parms.testAllRunning = testAllRunning
        parms.externalPythonInvocation = externalPythonInvocation
        parms.ShowConsoleLog = optionsForm.ShowConsoleLog.Checked

        parms.intrinsicsLeft = camera.intrinsicsLeft_VB
        parms.intrinsicsRight = camera.intrinsicsRight_VB
        parms.extrinsics = camera.Extrinsics_VB
        parms.homeDir = HomeDir.FullName

        PausePlayButton.Image = PausePlay

        Dim imgSize = New cv.Size(CInt(workingRes.Width * 2), CInt(workingRes.Height))
        imgResult = New cv.Mat(imgSize, cv.MatType.CV_8UC3, 0)

        Thread.CurrentThread.Priority = ThreadPriority.Lowest
        algorithmTaskHandle = New Thread(AddressOf AlgorithmTask)
        algorithmTaskHandle.Name = AvailableAlgorithms.Text
        algorithmTaskHandle.Start(parms)
    End Sub
    Private Sub AlgorithmTask(ByVal parms As VB_Classes.ActiveTask.algParms)
        Dim algName = algorithmTaskHandle.Name
        SyncLock algorithmThreadLock ' the duration of any algorithm varies a lot so wait here if previous algorithm is not finished.
            AlgorithmTestCount += 1
            drawRect = New cv.Rect
            Dim myLocation = New cv.Rect(Me.Left, Me.Top, Me.Width, Me.Height)
            Dim task = New VB_Classes.ActiveTask(parms, workingRes, algName, workingRes.Width, workingRes.Height, myLocation)
            textDesc = task.desc
            openFileInitialDirectory = task.openFileInitialDirectory
            openFileDialogRequested = task.openFileDialogRequested
            openFileinitialStartSetting = task.initialStartSetting
            task.fileStarted = task.initialStartSetting
            openFileStarted = task.initialStartSetting
            openFileFilterIndex = task.openFileFilterIndex
            openFileFilter = task.openFileFilter
            openFileDialogName = task.openFileDialogName
            openfileDialogTitle = task.openFileDialogTitle
            intermediateReview = ""

            Console.WriteLine(vbCrLf + vbCrLf + vbTab + algName + " " + textDesc + vbCrLf + vbTab + CStr(AlgorithmTestCount) + vbTab + "Algorithms tested")
            Console.WriteLine(vbTab + Format(totalBytesOfMemoryUsed, "#,##0") + "Mb working set before running " + algName +
                                      " with " + CStr(Process.GetCurrentProcess().Threads.Count) + " threads")
            Console.WriteLine(vbTab + "Active camera = " + camera.deviceName + " at resolution " + CStr(workingRes.Width) + "x" + CStr(workingRes.Height) + vbCrLf)

            ' if the constructor for the algorithm sets the drawrect, adjust it for the ratio of the actual size and algorithm sized image.
            If task.drawRect <> New cv.Rect Then
                drawRect = task.drawRect
                Dim ratio = task.color.Width / camPic(0).Width  ' relative size of algorithm size image to displayed image
                drawRect = New cv.Rect(drawRect.X / ratio, drawRect.Y / ratio, drawRect.Width / ratio, drawRect.Height / ratio)
            End If

            ttTextData.Clear()

            BothFirstAndLastReady = False
            frameCount = 0 ' restart the count...

            Run(task, algName)

            task.Dispose()
            If parms.testAllRunning Then Console.WriteLine(vbTab + "Ending " + algName)
        End SyncLock
        frameCount = 0
    End Sub
    Private Sub Run(task As VB_Classes.ActiveTask, algName As String)
        While 1
            Dim ratioImageToCampic = task.color.Width / camPic(0).Width  ' relative size of displayed image and algorithm size image.
            Dim currentCameraIndex = saveCameraIndex
            While 1
                Application.DoEvents()
                If saveAlgorithmName <> algName Then Exit Sub ' pause will stop the current algorithm as well.
                SyncLock bufferLock
                    If frameCount > 0 And (saveCameraIndex <> currentCameraIndex Or camera.width <> workingRes.Width) Then Exit Sub
                    If taskNewImages And pauseAlgorithmThread = False Then
                        ' bring the data into the algorithm task.
                        task.color = camera.color.clone
                        task.RGBDepth = camera.RGBDepth.clone
                        task.leftView = camera.leftView.clone
                        task.rightView = camera.rightView.clone

                        camera.depth16.convertto(task.depth32f, cv.MatType.CV_32F)
                        task.pointCloud = camera.PointCloud.clone

                        task.transformationMatrix = camera.transformationMatrix
                        task.IMU_TimeStamp = camera.IMU_TimeStamp
                        task.IMU_Barometer = camera.IMU_Barometer
                        task.IMU_Magnetometer = camera.IMU_Magnetometer
                        task.IMU_Temperature = camera.IMU_Temperature
                        task.IMU_Rotation = camera.IMU_Rotation
                        task.IMU_Translation = camera.IMU_Translation
                        task.IMU_Acceleration = camera.IMU_Acceleration
                        task.IMU_Velocity = camera.IMU_Velocity
                        task.IMU_AngularAcceleration = camera.IMU_AngularAcceleration
                        task.IMU_AngularVelocity = camera.IMU_AngularVelocity
                        task.IMU_FrameTime = camera.IMU_FrameTime
                        task.CPU_TimeStamp = camera.CPU_TimeStamp
                        task.CPU_FrameTime = camera.CPU_FrameTime
                        task.intermediateReview = intermediateReview
                        task.ratioImageToCampic = ratioImageToCampic
                        task.pixelViewerOn = If(testAllRunning, False, pixelViewerOn)

                        If GrabRectangleData Then
                            GrabRectangleData = False
                            Dim ratio = ratioImageToCampic
                            task.drawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                            If task.drawRect.Width <= 2 Then task.drawRect.Width = 0 ' too small?
                            Dim w = task.color.Width
                            If task.drawRect.X > w Then task.drawRect.X -= w
                            If task.drawRect.X < w And task.drawRect.X + task.drawRect.Width > w Then
                                task.drawRect.Width = w - task.drawRect.X
                            End If
                            BothFirstAndLastReady = False
                        End If

                        If ignoreMouseMove = False Then
                            task.mousePoint = mousePoint
                            task.mousePicTag = mousePicTag
                            task.mouseClickFlag = mouseClickFlag
                            If mouseClickFlag Then task.mouseClickPoint = mousePoint
                            mouseClickFlag = False
                        End If
                        task.fileStarted = openFileStarted ' UI may have stopped play.
                        taskNewImages = False
                        Exit While
                    End If
                End SyncLock
            End While

            task.RunAlgorithm()

            If task.mousePointUpdated Then mousePoint = task.mousePoint ' in case the algorithm has changed the mouse location...
            If task.drawRectUpdated Then drawRect = task.drawRect
            If task.drawRectClear Then
                drawRect = New cv.Rect
                task.drawRect = drawRect
                task.drawRectClear = False
            End If

            pixelViewerRect = task.pixelViewerRect
            pixelViewTag = task.pixelViewTag

            If openFileDialogName <> "" Then
                If openFileDialogName <> task.openFileDialogName Or openFileStarted <> task.fileStarted Then
                    task.fileStarted = openFileStarted
                    task.openFileDialogName = openFileDialogName
                End If
                openfileSliderPercent = task.openFileSliderPercent
            End If

            Static inputFile As String = "" ' task.openFileDialogName
            If inputFile <> task.openFileDialogName Then
                inputFile = task.openFileDialogName
                openFileInitialDirectory = task.openFileInitialDirectory
                openFileDialogRequested = task.openFileDialogRequested
                openFileinitialStartSetting = True ' if the file playing changes while the algorithm is running, automatically start playing the new file.
                openFileFilterIndex = task.openFileFilterIndex
                openFileFilter = task.openFileFilter
                openFileDialogName = task.openFileDialogName
                openfileDialogTitle = task.openFileDialogTitle
            End If

            If frameCount = 0 Then meActivateNeeded = True

            picLabels(2) = task.label1
            picLabels(3) = task.label2

            ' share the results of the algorithm task.
            SyncLock ttTextData
                If task.ttTextData.Count Then
                    ttTextData = New List(Of VB_Classes.TTtext)(task.ttTextData)
                    task.ttTextData.Clear()
                End If
            End SyncLock

            SyncLock imgResult
                imgResult = task.result.Clone()
                algorithmRefresh = True
            End SyncLock

            If Me.IsDisposed Then Exit While

            If frameCount Mod 100 = 0 Then
                SyncLock callTraceLock
                    ' this allows for dynamic allocation of new algorithms.
                    callTrace.Clear()
                    For i = 0 To task.callTrace.Count - 1
                        callTrace.Add(task.callTrace(i))
                    Next
                End SyncLock
            End If
            frameCount += 1
        End While
    End Sub
    Private Sub AvailableAlgorithms_DropDown(sender As Object, e As EventArgs) Handles AvailableAlgorithms.DropDown
        dropDownActive = True
    End Sub
    Private Sub AvailableAlgorithms_DropDownClosed(sender As Object, e As EventArgs) Handles AvailableAlgorithms.DropDownClosed
        dropDownActive = False
    End Sub
    Private Sub CreateSurveyImagesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CreateSurveyImagesToolStripMenuItem.Click
        surveyDir = New DirectoryInfo(HomeDir.FullName + "Survey/")
        If surveyDir.Exists = False Then surveyDir.Create()
        surveyActive = True
        nextSurveyImageAvailable = False
        AvailableAlgorithms.SelectedIndex = 0
    End Sub
    Private Sub SurveyTimer_Tick(sender As Object, e As EventArgs) Handles SurveyTimer.Tick
        If nextSurveyImageAvailable Then
            SurveyTimer.Enabled = False
            Dim dst = lastSurveyImage
            Dim dst1 = dst(New cv.Rect(0, 0, dst.Width / 2, dst.Height))
            Dim dst2 = dst(New cv.Rect(dst.Width / 2, 0, dst.Width / 2, dst.Height))

            Dim encodeParams() As Integer = {cv.ImwriteFlags.JpegQuality, 99}
            Dim minVal As Double, maxVal As Double
            cv.Cv2.MinMaxLoc(dst1, minVal, maxVal)
            If maxVal > 0 Then cv.Cv2.ImWrite(surveyDir.FullName + "/" + AvailableAlgorithms.Text + "1.jpg", dst1, encodeParams)

            cv.Cv2.MinMaxLoc(dst2, minVal, maxVal)
            If maxVal > 0 Then cv.Cv2.ImWrite(surveyDir.FullName + "/" + AvailableAlgorithms.Text + "2.jpg", dst2, encodeParams)

            If AvailableAlgorithms.SelectedIndex = AvailableAlgorithms.Items.Count - 1 Then
                surveyActive = False
                AvailableAlgorithms.SelectedIndex = 0
            Else
                AvailableAlgorithms.SelectedIndex += 1
            End If
            Thread.Sleep(2000)
            nextSurveyImageAvailable = False
        End If
    End Sub
End Class

