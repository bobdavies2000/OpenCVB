Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://learnopencvb.com/object-tracking-using-opencv-cpp-python/
Public Class Tracker_Basics : Inherits TaskParent
    Public tRect As cv.Rect
    Dim saveRect As New cv.Rect
    Dim options As New Options_Tracker
    Public Sub New()
        If task.testAllRunning Then task.drawRect = New cv.Rect(25, 25, 25, 25)
        desc = "Use C++ to track objects.  Results are poor compared to Match_DrawRect"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Channels() <> 1 Then src = task.gray

        If task.optionsChanged Then
            If cPtr <> 0 Then Tracker_Basics_Close(cPtr)
            cPtr = Tracker_Basics_Open(options.trackType)
            saveRect = task.drawRect
        End If

        If saveRect.Width <> 0 Then
            Dim dataSrc(src.Total * src.ElemSize) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim r = saveRect
            Dim imagePtr = Tracker_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, r.X, r.Y, r.Width, r.Height)
            handleSrc.Free()

            dst2 = src
            Dim rectData(4 - 1) As Integer
            Marshal.Copy(imagePtr, rectData, 0, rectData.Length)

            tRect = New cv.Rect(rectData(0), rectData(1), rectData(2), rectData(3))
            dst2.Rectangle(tRect, white, task.lineWidth)
        Else
            SetTrueText("Draw a rectangle around any object to be tracked in the BGR image above.", New cv.Point(10, 140))
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Tracker_Basics_Close(cPtr)
    End Sub
End Class






Public Class Tracker_Correlation : Inherits TaskParent
    Dim track As New Tracker_Basics
    Dim match As New Match_Basics
    Dim matchRect As cv.Rect
    Public Sub New()
        desc = "Use Tracker_Basics to initialize and then use correlation to keep tracking."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static runTrackerFlag As Boolean = True
        Static trackRect = track.tRect, trackRuns As Integer, totalRuns As Integer

        totalRuns += 1
        If runTrackerFlag Then
            track.Run(src)
        End If
        If trackRect <> track.tRect Then
            trackRect = track.tRect
            match.template = task.gray(trackRect)
            matchRect = trackRect
            runTrackerFlag = False
            trackRuns += 1
        End If
        If matchRect.Width > 0 Then
            match.Run(task.gray(matchRect))
            matchRect = trackRect
            If match.correlation < 0.98 Then runTrackerFlag = True
        End If
        labels(2) = "Correlation = " + Format(match.correlation, fmt3) + ".  TrackRuns% = " + Format(trackRuns / totalRuns, "0%")
        dst2 = track.dst2
        task.drawRect = New cv.Rect
    End Sub
End Class






Public Class Tracker_GravityLine : Inherits TaskParent
    Public Sub New()
        desc = "Track the gravity line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
    End Sub
End Class
