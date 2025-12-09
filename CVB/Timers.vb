Imports cv = OpenCvSharp
Namespace MainForm
    Partial Public Class MainForm
        Public frameCount As Integer
        Dim fpsWriteCount As Integer
        Dim fpsListA As New List(Of Single)
        Dim fpsListC As New List(Of Single)
        Dim totalBytesOfMemoryUsed As Integer
        Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
            Static lastTime As DateTime = Now
            Static lastAlgorithmFrame As Integer
            Static lastCameraFrame As Integer
            If camera Is Nothing Then Exit Sub
            If lastAlgorithmFrame > frameCount Then lastAlgorithmFrame = 0
            If lastCameraFrame > camera.cameraFrameCount Then lastCameraFrame = 0

            If isPlaying And myTask IsNot Nothing Then
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

                CameraSwitching.Text = AvailableAlgorithms.Text + " awaiting first buffer"
                Dim cameraName = settings.cameraName
                cameraName = cameraName.Replace(" 2/2i", "")
                cameraName = cameraName.Replace(" camera", "")
                cameraName = cameraName.Replace(" Camera", "")
                cameraName = cameraName.Replace("Intel(R) RealSense(TM) Depth ", "Intel D")

                myTask.fpsAlgorithm = fpsListA.Average
                myTask.fpsCamera = CInt(fpsListC.Average)
                If myTask.fpsAlgorithm >= 100 Then myTask.fpsAlgorithm = 99
                If myTask.fpsCamera >= 100 Then myTask.fpsCamera = 99
                If fpsListA.Count > 5 Then
                    fpsListA.RemoveAt(0)
                    fpsListC.RemoveAt(0)
                End If

                If myTask.fpsAlgorithm = 0 Then
                    myTask.fpsAlgorithm = 1
                Else
                    If myTask.testAllRunning Then
                        Static lastWriteTime = timeNow
                        elapsedTime = timeNow.Ticks - lastWriteTime.Ticks
                        spanCopy = New TimeSpan(elapsedTime)
                        taskTimerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                        If taskTimerInterval > If(myTask.testAllRunning, 1000, 5000) Then
                            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
                            totalBytesOfMemoryUsed = currentProcess.PrivateMemorySize64 / (1024 * 1024)

                            lastWriteTime = timeNow
                            If fpsWriteCount = 5 Then
                                Debug.WriteLine("")
                                fpsWriteCount = 0
                            End If
                            fpsWriteCount += 1
                            Debug.Write(" " + Format(totalBytesOfMemoryUsed, "###") + "/" +
                                              Format(myTask.fpsAlgorithm, fmt0) + "/" + Format(myTask.fpsCamera, fmt0))
                        End If
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace