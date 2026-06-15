Imports cv = OpenCvSharp
Public Class HeartBeat_Basics_TA : Inherits TaskParent
    Public Sub New()
        desc = "Update the heart beat variables"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.myStopWatch Is Nothing Then task.myStopWatch = Stopwatch.StartNew()

        ' update the time measures
        Dim msWatch = task.myStopWatch.ElapsedMilliseconds

        task.quarterBeat = False
        task.midHeartBeat = False
        task.heartBeat = False
        Static msLast As Integer
        Dim ms = (msWatch - msLast) / 1000
        For i = 0 To task.quarter.Count - 1
            If task.quarter(i) = False And ms > Choose(i + 1, 0.25, 0.5, 0.75, 1.0) Then
                task.quarterBeat = True
                If i = 1 Then task.midHeartBeat = True
                If i = 3 Then task.heartBeat = True
                task.quarter(i) = True
            End If
        Next
        If task.heartBeat Then ReDim task.quarter(4)
        If task.heartBeat Then task.heartbeatFrame = task.frameCount

        If task.frameCount = 1 Then task.heartBeat = True

        If task.heartBeat Then
            task.heartBeatCount += 1
            If task.heartBeatCount Mod 5 = 0 Then task.heartBeatLT = True
        End If

        Dim frameDuration = 1000 / task.fpsAlgorithm
        task.almostHeartBeat = If(msWatch - msLast + frameDuration * 1.5 > 1000, True, False)

        If (msWatch - msLast) > 1000 Then msLast = msWatch
        If task.heartBeatLT Then task.toggleOn = Not task.toggleOn

        Static lastHeartBeatLT As Boolean = False
        Static lastHeartBeat As Boolean = False

        task.afterHeartBeat = If(lastHeartBeat, True, False)
        task.afterHeartBeatLT = If(lastHeartBeatLT, True, False)

        lastHeartBeat = task.heartBeat
        lastHeartBeatLT = task.heartBeatLT

        task.metersPerPixel = task.MaxZmeters / task.workRes.Height ' meters per pixel in projections - side and top.
    End Sub
End Class
