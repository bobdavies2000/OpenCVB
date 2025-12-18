Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports System.Windows
' https://learnopencvb.com/object-tracking-using-opencv-cpp-python/
Namespace VBClasses
    Public Class Track_Basics : Inherits TaskParent
        Public outputRect As cv.Rect
        Public inputRect As cv.Rect
        Dim options As New Options_Tracker
        Dim track As New Track_BasicsQT
        Public Sub New()
            If standalone Then algTask.drawRect = New cv.Rect(25, 25, 25, 25)
            desc = "Use C++ to track objects.  Results are poor compared to Match_DrawRect"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            If standalone Then inputRect = algTask.drawRect

            track.inputRect = inputRect
            track.trackerIndex = options.trackType
            track.Run(algTask.gray)
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
            If algTask.drawRect.Width = 0 Or algTask.drawRect.Height = 0 Then
                algTask.drawRect = New cv.Rect(25, 25, 25, 25)
            End If
            desc = "Use C++ to track objects.  Results are poor compared to Match_DrawRect"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If algTask.optionsChanged Or cPtr = 0 Then
                If cPtr <> 0 Then Track_Basics_Close(cPtr)
                cPtr = Track_Basics_Open(trackerIndex)
                If standalone Then inputRect = algTask.drawRect
            End If

            Dim dataSrc(src.Total) As Byte
            Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)
            Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
            Dim r = inputRect
            If r.Width = 0 Or r.Height = 0 Then r = algTask.drawRect
            Dim imagePtr = Track_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, r.X, r.Y, r.Width, r.Height)
            handleSrc.Free()

            dst2 = src
            Dim rectData(4 - 1) As Integer
            Marshal.Copy(imagePtr, rectData, 0, rectData.Length)

            outputRect = New cv.Rect(rectData(0), rectData(1), rectData(2), rectData(3))
            dst2.Rectangle(outputRect, white, algTask.lineWidth)
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
            Dim lpList = algTask.lines.lpList

            If algTask.heartBeatLT Then
                algTask.optionsChanged = True
                Dim gridIndex = algTask.gridMap.Get(Of Integer)(lpList(0).p1.Y, lpList(0).p1.X)
                track.inputRect = algTask.gridNabeRects(gridIndex)
                dst3.SetTo(0)
                dst3(track.inputRect) = src(track.inputRect).Clone
            End If
            track.Run(algTask.gray)
            dst2 = track.dst2
        End Sub
    End Class






    Public Class Track_GridRect : Inherits TaskParent
        Dim track As New Track_Basics
        Public Sub New()
            desc = "Track the gravity RGB vector"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lpList = algTask.lines.lpList

            Static searchRect As cv.Rect, originalRect As cv.Rect
            If searchRect.Width = 0 Or searchRect.Height = 0 Then
                Dim gridIndex = algTask.gridMap.Get(Of Integer)(lpList(0).p1.Y, lpList(0).p1.X)
                originalRect = algTask.gridRects(gridIndex)
                searchRect = algTask.gridNabeRects(gridIndex)
                Dim x = originalRect.X - searchRect.X
                Dim y = originalRect.Y - searchRect.Y
                track.inputRect = New cv.Rect(x, y, algTask.brickSize, algTask.brickSize)
                dst3 = src
                dst3.Rectangle(searchRect, white, algTask.lineWidth)
                dst3.Rectangle(originalRect, white, algTask.lineWidth)
            End If
            track.Run(algTask.gray(searchRect))
            dst2 = src
            Dim r = New cv.Rect(originalRect.X + track.outputRect.X, originalRect.Y + track.outputRect.Y, algTask.brickSize, algTask.brickSize)
            dst2.Rectangle(r, white, algTask.lineWidth)
            dst3.Rectangle(r, white, algTask.lineWidth)
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
            Dim lpList = algTask.lines.lpList

            Dim trackCount = Math.Min(track.Length, lpList.Count)
            dst2 = src
            For i = 0 To trackCount - 1
                If algTask.heartBeat Then
                    Dim gridIndex = algTask.gridMap.Get(Of Integer)(lpList(i).p1.Y, lpList(i).p1.X)
                    track(i).inputRect = algTask.gridNabeRects(gridIndex)
                End If
                track(i).Run(algTask.gray)
                dst2.Rectangle(track(i).outputRect, algTask.highlight, algTask.lineWidth)
            Next
            labels(2) = "Tracking the top " + CStr(track.Length) + " line endpoints"
        End Sub
    End Class

End Namespace