Imports VBClasses
Namespace MainUI
    Partial Public Class MainUI
        Dim fpsWriteCount As Integer
        Dim totalBytesOfMemoryUsed As Integer
        Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
            If taskAlg Is Nothing Then Exit Sub
            Static lastTime As DateTime = Now
            Static lastAlgorithmFrame As Integer
            Static lastCameraFrame As Integer
            If camera Is Nothing Then Exit Sub
            If lastAlgorithmFrame > taskAlg.frameCount Then lastAlgorithmFrame = 0
            If lastCameraFrame > camera.cameraFrameCount Then lastCameraFrame = 0

            If isPlaying Then
                Dim timeNow As DateTime = Now
                Dim elapsedTime = timeNow.Ticks - lastTime.Ticks
                Dim spanCopy As TimeSpan = New TimeSpan(elapsedTime)
                Dim taskTimerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                If taskTimerInterval = 0 Then Exit Sub
                lastTime = timeNow

                Dim countFrames = taskAlg.frameCount - lastAlgorithmFrame
                Dim camFrames = camera.cameraFrameCount - lastCameraFrame
                lastAlgorithmFrame = taskAlg.frameCount
                lastCameraFrame = camera.cameraFrameCount

                taskAlg.fpsCamera = camFrames / (taskTimerInterval / 1000)
                If taskAlg.fpsCamera >= 100 Then taskAlg.fpsCamera = 99

                taskAlg.fpsAlgorithm = countFrames / (taskTimerInterval / 1000)
                If taskAlg.fpsAlgorithm >= 100 Then taskAlg.fpsAlgorithm = 99

                If TestAllTimer.Enabled Then
                    Static lastWriteTime = timeNow
                    elapsedTime = timeNow.Ticks - lastWriteTime.Ticks
                    spanCopy = New TimeSpan(elapsedTime)
                    taskTimerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                    If taskTimerInterval > 5000 Then
                        Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
                        totalBytesOfMemoryUsed = currentProcess.PrivateMemorySize64 / (1024 * 1024)

                        lastWriteTime = timeNow
                        If fpsWriteCount = 5 Then
                            Debug.WriteLine("")
                            fpsWriteCount = 0
                        End If
                        fpsWriteCount += 1
                        Debug.Write(" " + Format(totalBytesOfMemoryUsed, "###") + "/" +
                                              Format(taskAlg.fpsAlgorithm, "0") + "/" + Format(taskAlg.fpsCamera, "0"))
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace