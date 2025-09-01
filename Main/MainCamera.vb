Imports System.IO
Imports System.Threading
Imports cv = OpenCvSharp
Namespace OpenCVB
    Partial Class Main
        Dim camera As Object
        Dim cameraTaskHandle As Thread
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
        Private Sub setupCameraPath()
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
        Private Sub initCamera()
            CameraSwitching.Text = settings.cameraName + " starting"
            Application.DoEvents()
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
                Case "Intel(R) RealSense(TM) Depth Camera 455", "Intel(R) RealSense(TM) Depth Camera 435i"
                    Return New CameraRS2(settings.workRes, settings.captureRes, settings.cameraName)
                'Case "Oak-D camera"
                '    Return New CameraOakD_CPP(settings.workRes, settings.captureRes, settings.cameraName)
                Case "StereoLabs ZED 2/2i"
                    Return New CameraZed2(settings.workRes, settings.captureRes, settings.cameraName)
                Case "Orbbec Gemini 335L", "Orbbec Gemini 336L", "Orbbec Gemini 335"
                    Return New CameraORB(settings.workRes, settings.captureRes, settings.cameraName)
            End Select
            Return New CameraRS2(settings.workRes, settings.captureRes, settings.cameraName)
        End Function
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
                    End
                End If
            End While
        End Sub
    End Class
End Namespace