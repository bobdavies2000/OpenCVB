Imports cv = OpenCvSharp
Imports System.Threading
Imports VBClasses

Namespace OpenCVB
    Partial Class Main
        Dim saveDrawRect As cv.Rect
        Dim ratio As Single
        Dim algName As String
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
        Private Function getCalibdata(ByRef calibdata As VBtask.cameraInfo) As Boolean
            While 1
                If camera IsNot Nothing Then
                    calibdata = setCalibData(camera.calibData)
                    Exit While
                ElseIf saveAlgorithmName = "" Then
                    Return False
                End If
            End While
            Return True
        End Function
        Private Sub logAlgorithm()
            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
            totalBytesOfMemoryUsed = currentProcess.PrivateMemorySize64 / (1024 * 1024)

            Debug.WriteLine(vbTab + Format(totalBytesOfMemoryUsed, "#,##0") + "Mb working set before running " +
                         algName + " with " + CStr(Process.GetCurrentProcess().Threads.Count) + " active threads")

            Debug.WriteLine(vbTab + "Pool thread count = " + CStr(Threading.ThreadPool.ThreadCount))

            Debug.WriteLine(vbTab + "Active camera = " + settings.cameraName)
            Debug.WriteLine(vbTab + "Input resolution " + CStr(settings.captureRes.Width) + "x" +
                                                      CStr(settings.captureRes.Height))
            Debug.WriteLine(vbTab + "Working resolution of " + CStr(settings.workRes.Width) + "x" +
                                                           CStr(settings.workRes.Height))
            Debug.WriteLine("")
            Debug.WriteLine(" MemUsage/FPSAlg/FPSCam ")
        End Sub
        Private Sub waitForCamera()
            While 1
                ' exit the inner while if any of these change.
                If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Or cameraShutdown Then Exit While
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
                        task.color = camera.uiColor.clone
                        task.leftView = camera.uiLeft.clone
                        task.rightView = camera.uiRight.clone
                        task.pointCloud = camera.uiPointCloud.clone

                        ' there might be a delay in the camera task so set it again here....
                        If frameCount < 10 Then task.calibData = setCalibData(camera.calibData)

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

                    If GrabRectangleData Then
                        GrabRectangleData = False
                        Dim tmpDrawRect = New cv.Rect(drawRect.X / ratio, drawRect.Y / ratio, drawRect.Width / ratio,
                                              drawRect.Height / ratio)
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
        End Sub
        Private Sub initializeResultMats()
            ReDim results.dstList(3)
            For i = 0 To results.dstList.Count - 1
                results.dstList(i) = New cv.Mat
            Next
            If algName = "GL_MainForm" Then
                results.GLcloud = New cv.Mat
                results.GLrgb = New cv.Mat
            End If
        End Sub
        Private Sub AlgorithmTask(ByVal parms As VBClasses.VBtask.algParms)
            If parms.algName = "" Then Exit Sub
            algName = parms.algName
            algorithmQueueCount += 1
            fpsAlgorithm = 0
            newCameraImages = False

            initializeResultMats()

            If getCalibdata(parms.calibData) = False Then Exit Sub

            ' During overnight testing, the duration of any algorithm varies a lot.
            ' Wait here if previous algorithm is not finished.
            SyncLock algorithmThreadLock
                fpsWriteCount = 0

                algorithmQueueCount -= 1
                AlgorithmTestAllCount += 1
                drawRect = New cv.Rect
                task = New VBtask(parms)
                SyncLock trueTextLock
                    trueData = New List(Of TrueText)
                End SyncLock

                task.lowResDepth = New cv.Mat(task.workRes, cv.MatType.CV_32F)
                task.lowResColor = New cv.Mat(task.workRes, cv.MatType.CV_32F)
                task.MainUI_Algorithm = algolist.createAlgorithm(algName)
                AlgDescription.Text = task.MainUI_Algorithm.desc

                If ComplexityTimer.Enabled = False Then logAlgorithm()

                ' Adjust drawrect for the ratio of the actual size and workRes.
                ratio = camPic(0).Width / task.workRes.Width
                If task.drawRect <> New cv.Rect Then
                    ' relative size of algorithm size image to displayed image
                    drawRect = New cv.Rect(task.drawRect.X * ratio, task.drawRect.Y * ratio,
                                   task.drawRect.Width * ratio, task.drawRect.Height * ratio)
                End If

                Dim saveworkRes = settings.workRes
                mousePointCamPic = New cv.Point(task.workRes.Width / 2, task.workRes.Height / 2) ' mouse click point default = center of the image

                task.motionMask = New cv.Mat(task.workRes, cv.MatType.CV_8U, 255)
                task.leftView = New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
                task.rightView = New cv.Mat(task.workRes, cv.MatType.CV_8U, 0)
                While 1
                    Dim waitTime = Now
                    waitForCamera()
                    Dim endWaitTime = Now

                    Dim elapsedWaitTicks = endWaitTime.Ticks - waitTime.Ticks
                    Dim spanWait = New TimeSpan(elapsedWaitTicks)
                    Dim spanTime = TimeSpan.TicksPerMillisecond - task.inputBufferCopy
                    If spanTime = 0 Then
                        task.waitingForInput = 0
                    Else
                        task.waitingForInput = spanWait.Ticks / TimeSpan.TicksPerMillisecond - task.inputBufferCopy
                    End If

                    ' exit the outer while if any of these change.
                    If cameraTaskHandle Is Nothing Or algorithmQueueCount > 0 Or cameraShutdown Then Exit While
                    If saveworkRes <> settings.workRes Then Exit While
                    If saveCameraName <> settings.cameraName Then Exit While
                    If saveAlgorithmName <> task.algName Then Exit While

                    If activeMouseDown = False Then
                        SyncLock mouseLock
                            If mousePointCamPic.X < 0 Then mousePointCamPic.X = 0
                            If mousePointCamPic.Y < 0 Then mousePointCamPic.Y = 0
                            If mousePointCamPic.X >= task.workRes.Width Then mousePointCamPic.X = task.workRes.Width - 1
                            If mousePointCamPic.Y >= task.workRes.Height Then mousePointCamPic.Y = task.workRes.Height - 1

                            task.mouseMovePoint = mousePointCamPic
                            If task.mouseMovePoint = New cv.Point(0, 0) Then
                                task.mouseMovePoint = New cv.Point(task.workRes.Width / 2, task.workRes.Height / 2)
                            End If
                            task.mouseMovePoint = validatePoint(task.mouseMovePoint)
                            task.mousePicTag = mousePicTag
                            If task.ClickPoint = New cv.Point Then task.ClickPoint = New cv.Point(task.workRes.Width / 2, task.workRes.Height / 2)
                            If mouseClickFlag Then
                                task.mouseClickFlag = mouseClickFlag
                                task.ClickPoint = mousePointCamPic
                                ClickPoint = task.ClickPoint
                                mouseClickFlag = False
                            End If
                        End SyncLock
                    End If

                    If activateTaskForms Then
                        task.activateTaskForms = True
                        activateTaskForms = False
                    End If

                    Dim updatedDrawRect = task.drawRect
                    task.fpsCamera = fpsCamera

                    If testAllRunning Then
                        task.pixelViewerOn = False
                    Else
                        task.pixelViewerOn = pixelViewerOn
                    End If



                    task.RunAlgorithm() ' <<<<<<<<<<< this is where the real work gets done.



                    picLabels = task.labels

                    SyncLock mouseLock
                        mousePointCamPic = validatePoint(mousePointCamPic)
                        mouseMovePoint = validatePoint(New cv.Point(task.mouseMovePoint.X * ratio, task.mouseMovePoint.Y * ratio))
                    End SyncLock

                    Dim returnTime = Now

                    ' in case the algorithm has changed the mouse location...
                    If task.mouseMovePointUpdated Then mousePointCamPic = task.mouseMovePoint
                    If updatedDrawRect <> task.drawRect Then
                        drawRect = task.drawRect
                        drawRect = New cv.Rect(drawRect.X * ratio, drawRect.Y * ratio, drawRect.Width * ratio, drawRect.Height * ratio)
                    End If
                    If task.drawRectClear Then
                        drawRect = New cv.Rect
                        task.drawRect = drawRect
                        task.drawRectClear = False
                    End If

                    pixelViewerRect = task.pixelViewerRect
                    pixelViewTag = task.pixelViewTag

                    If Single.IsNaN(fpsAlgorithm) Or fpsAlgorithm = 0 Then
                        task.fpsAlgorithm = 1
                    Else
                        task.fpsAlgorithm = If(fpsAlgorithm < 0.01, 1, fpsAlgorithm)
                    End If

                    Dim ptCursor = New cv.Point(mouseMovePoint.X / ratio, mouseMovePoint.Y / ratio)
                    SyncLock trueTextLock
                        trueData.Clear()
                        If task.trueData.Count Then
                            trueData = New List(Of VBClasses.TrueText)(task.trueData)
                        End If
                        task.trueData.Clear()
                    End SyncLock

                    If task.displayDst1 = False Or task.labels(1) = "" Then picLabels(1) = "DepthRGB"
                    picLabels(1) = task.depthAndCorrelationText.Replace(vbCrLf, "")

                    Dim elapsedTicks = Now.Ticks - returnTime.Ticks
                    Dim span = New TimeSpan(elapsedTicks)
                    task.returnCopyTime = span.Ticks / TimeSpan.TicksPerMillisecond

                    task.mouseClickFlag = False
                    frameCount = task.frameCount
                    ' this can be very useful.  When debugging your algorithm, turn this global option on to sync output to debug.
                    ' Each image will represent the one just finished by the algorithm.
                    If task.debugSyncUI Then Thread.Sleep(100)
                    If task.closeRequest Then
                        cameraShutdown = True
                        Exit While
                    End If

                    algorithmRefresh = True
                    paintNewImages = True ' trigger the paint 

                    SyncLock task.resultLock
                        For i = 0 To task.results.dstList.Count - 1
                            results.dstList(i) = task.results.dstList(i).Clone
                        Next
                        If parms.algName = "GL_MainForm" Then
                            results.GLRequest = task.results.GLRequest
                            results.GLcloud = task.pointCloud.Clone
                            results.GLrgb = task.color.Clone
                        End If
                    End SyncLock
                End While

                task.frameCount = -1
                task.Dispose()
                fpsWriteCount = 0
            End SyncLock

            frameCount = 0
        End Sub
    End Class
End Namespace