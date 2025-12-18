Imports cv = OpenCvSharp
Imports VBClasses
Namespace MainUI
    Partial Public Class MainUI
        Dim fpsWriteCount As Integer
        Dim fpsListA As New List(Of Single)
        Dim fpsListC As New List(Of Single)
        Dim totalBytesOfMemoryUsed As Integer
        Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
            If task Is Nothing Then Exit Sub
            Static lastTime As DateTime = Now
            Static lastAlgorithmFrame As Integer
            Static lastCameraFrame As Integer
            If camera Is Nothing Then Exit Sub
            If lastAlgorithmFrame > task.frameCount Then lastAlgorithmFrame = 0
            If lastCameraFrame > camera.cameraFrameCount Then lastCameraFrame = 0

            If isPlaying And task IsNot Nothing Then
                Dim timeNow As DateTime = Now
                Dim elapsedTime = timeNow.Ticks - lastTime.Ticks
                Dim spanCopy As TimeSpan = New TimeSpan(elapsedTime)
                Dim taskTimerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                lastTime = timeNow

                Dim countFrames = task.frameCount - lastAlgorithmFrame
                Dim camFrames = camera.cameraFrameCount - lastCameraFrame
                lastAlgorithmFrame = task.frameCount
                lastCameraFrame = camera.cameraFrameCount

                If taskTimerInterval > 0 Then
                    Dim testVal = camFrames / (taskTimerInterval / 1000)
                    If testVal >= 200 Then testVal = 99
                    fpsListC.Add(testVal)

                    testVal = countFrames / (taskTimerInterval / 1000)
                    If testVal >= 200 Then testVal = 99
                    fpsListA.Add(testVal)
                End If

                task.fpsAlgorithm = fpsListA.Average
                task.fpsCamera = CInt(fpsListC.Average)

                If fpsListA.Count > 5 Then
                    fpsListA.RemoveAt(0)
                    fpsListC.RemoveAt(0)
                End If

                If task.fpsAlgorithm = 0 Then
                    task.fpsAlgorithm = 1
                Else
                    If task.testAllRunning Then
                        Static lastWriteTime = timeNow
                        elapsedTime = timeNow.Ticks - lastWriteTime.Ticks
                        spanCopy = New TimeSpan(elapsedTime)
                        taskTimerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                        If taskTimerInterval > If(task.testAllRunning, 1000, 5000) Then
                            Dim currentProcess = System.Diagnostics.Process.GetCurrentProcess()
                            totalBytesOfMemoryUsed = currentProcess.PrivateMemorySize64 / (1024 * 1024)

                            lastWriteTime = timeNow
                            If fpsWriteCount = 5 Then
                                Debug.WriteLine("")
                                fpsWriteCount = 0
                            End If
                            fpsWriteCount += 1
                            Debug.Write(" " + Format(totalBytesOfMemoryUsed, "###") + "/" +
                                              Format(task.fpsAlgorithm, "0") + "/" + Format(task.fpsCamera, "0"))
                        End If
                    End If
                End If
            End If
        End Sub
    End Class
End Namespace