Imports VBClasses
Namespace MainApp
    Partial Public Class MainUI
        Dim fpsWriteCount As Integer
        Dim totalBytesOfMemoryUsed As Integer
        Private Sub fpsTimer_Tick(sender As Object, e As EventArgs) Handles fpsTimer.Tick
            If vbc.task Is Nothing Then Exit Sub
            Static lastTime As DateTime = Now
            Static lastAlgorithmFrame As Integer
            Static lastCameraFrame As Integer
            If camera Is Nothing Then Exit Sub
            If lastAlgorithmFrame > vbc.task.frameCount Then lastAlgorithmFrame = 0
            If lastCameraFrame > camera.cameraFrameCount Then lastCameraFrame = 0

            If isPlaying Then
                Dim timeNow As DateTime = Now
                Dim elapsedTime = timeNow.Ticks - lastTime.Ticks
                Dim spanCopy As TimeSpan = New TimeSpan(elapsedTime)
                Dim taskTimerInterval = spanCopy.Ticks / TimeSpan.TicksPerMillisecond
                If taskTimerInterval = 0 Then Exit Sub
                lastTime = timeNow

                Dim algFrames = vbc.task.frameCount - lastAlgorithmFrame
                Dim camFrames = camera.cameraFrameCount - lastCameraFrame

                lastAlgorithmFrame = vbc.task.frameCount
                lastCameraFrame = camera.cameraFrameCount

                vbc.task.fpsCamera = camFrames / (taskTimerInterval / 1000)
                If vbc.task.fpsCamera >= 99 Then vbc.task.fpsCamera = 99

                vbc.task.fpsAlgorithm = algFrames / (taskTimerInterval / 1000)
                If vbc.task.fpsAlgorithm >= 99 Then vbc.task.fpsAlgorithm = 99
            End If
        End Sub
    End Class
End Namespace