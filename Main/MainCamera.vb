Imports System.Threading
Imports cv = OpenCvSharp
Namespace OpenCVB
    Partial Class Main
        Private Sub initCamera()
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