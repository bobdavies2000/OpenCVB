Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Windows
' https://learnopencvb.com/object-tracking-using-opencv-cpp-python/
Public Class Track_Basics : Inherits TaskParent
    Public outputRect As cv.Rect
    Public inputRect As cv.Rect
    Dim options As New Options_Tracker
    Dim track As New Track_BasicsQT
    Public Sub New()
        If standalone Then task.drawRect = New cv.Rect(25, 25, 25, 25)
        desc = "Use C++ to track objects.  Results are poor compared to Match_DrawRect"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        If standalone Then inputRect = task.drawRect

        track.inputRect = inputRect
        track.trackerIndex = options.trackType
        track.Run(task.gray)
        dst2 = track.dst2
        outputRect = track.outputRect
        SetTrueText("Draw a rectangle around any object to be tracked in the BGR image above.", 3)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Track_Basics_Close(cPtr)
    End Sub
End Class






Public Class Track_BasicsQT : Inherits TaskParent
    Public outputRect As cv.Rect
    Public inputRect As cv.Rect
    Public trackerIndex As Integer = 1 ' default is "MIL" 
    Public Sub New()
        If standalone Then task.drawRect = New cv.Rect(25, 25, 25, 25)
        desc = "Use C++ to track objects.  Results are poor compared to Match_DrawRect"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            If cPtr <> 0 Then Track_Basics_Close(cPtr)
            cPtr = Track_Basics_Open(trackerIndex)
            If standalone Then inputRect = task.drawRect
        End If

        Dim dataSrc(src.Total) As Byte
        Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
        Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
        Dim r = inputRect
        Dim imagePtr = Track_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, r.X, r.Y, r.Width, r.Height)
        handleSrc.Free()

        dst2 = src
        Dim rectData(4 - 1) As Integer
        Marshal.Copy(imagePtr, rectData, 0, rectData.Length)

        outputRect = New cv.Rect(rectData(0), rectData(1), rectData(2), rectData(3))
        dst2.Rectangle(outputRect, white, task.lineWidth)
        SetTrueText("Draw a rectangle around any object to be tracked in the BGR image above.", 3)
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Track_Basics_Close(cPtr)
    End Sub
End Class







Public Class Track_LongestLine : Inherits TaskParent
    Dim track As New Track_Basics
    Public Sub New()
        desc = "Track the longest RGB line"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lpList = task.lines.lpList
        If lpList.Count = 0 Then Exit Sub

        If task.heartBeatLT Then
            task.optionsChanged = True
            Dim gridIndex = task.gridMap.Get(Of Integer)(lpList(0).p1.Y, lpList(0).p1.X)
            track.inputRect = task.gridNabeRects(gridIndex)
            dst3.SetTo(0)
            dst3(track.inputRect) = src(track.inputRect).Clone
        End If
        track.Run(task.gray)
        dst2 = track.dst2
    End Sub
End Class






Public Class Track_GridRect : Inherits TaskParent
    Dim track As New Track_Basics
    Public Sub New()
        desc = "Track the gravity RGB vector"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lpList = task.lines.lpList
        If lpList.Count = 0 Then Exit Sub

        Static searchRect As cv.Rect, originalRect As cv.Rect
        If task.heartBeatLT Then
            Dim gridIndex = task.gridMap.Get(Of Integer)(lpList(0).p1.Y, lpList(0).p1.X)
            originalRect = task.gridRects(gridIndex)
            searchRect = task.gridNabeRects(gridIndex)
            Dim x = originalRect.X - searchRect.X
            Dim y = originalRect.Y - searchRect.Y
            track.inputRect = New cv.Rect(x, y, task.brickSize, task.brickSize)
            dst3 = src
            dst3.Rectangle(searchRect, white, task.lineWidth)
            dst3.Rectangle(originalRect, white, task.lineWidth)
        End If
        track.Run(task.gray(searchRect))
        dst2 = src
        Dim r = New cv.Rect(originalRect.X + track.outputRect.X, originalRect.Y + track.outputRect.Y, task.brickSize, task.brickSize)
        dst2.Rectangle(r, white, task.lineWidth)
        dst3.Rectangle(r, white, task.lineWidth)
    End Sub
End Class






Public Class Track_Lines : Inherits TaskParent
    Dim track(4) As Track_BasicsQT
    Public Sub New()
        For i = 0 To track.Length - 1
            track(i) = New Track_BasicsQT
        Next
        desc = "Track the top X lines using Track_Basics"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lpList = task.lines.lpList
        If lpList.Count = 0 Then Exit Sub

        Dim trackCount = Math.Min(track.Length, lpList.Count)
        dst2 = src
        For i = 0 To trackCount - 1
            If task.heartBeat Then
                Dim gridIndex = task.gridMap.Get(Of Integer)(lpList(i).p1.Y, lpList(i).p1.X)
                track(i).inputRect = task.gridNabeRects(gridIndex)
            End If
            track(i).Run(task.gray)
            dst2.Rectangle(track(i).outputRect, task.highlight, task.lineWidth)
        Next
        labels(2) = "Tracking the top " + CStr(track.Length) + " line endpoints"
    End Sub
End Class
