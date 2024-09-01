Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
' https://learnopencvb.com/object-tracking-using-opencv-cpp-python/
Public Class Tracker_Basics : Inherits VB_Parent
    Public tRect As cvb.Rect
    Dim saveRect As New cvb.Rect
    Dim options As New Options_Tracker
    Public Sub New()
        If task.testAllRunning Then task.drawRect = New cvb.Rect(25, 25, 25, 25)
        desc = "Use C++ to track objects.  Results are poor compared to Match_DrawRect"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        If task.drawRect <> saveRect Or tInfo.optionsChanged Then
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

            tRect = New cvb.Rect(rectData(0), rectData(1), rectData(2), rectData(3))
            dst2.Rectangle(tRect, cvb.Scalar.White, task.lineWidth)
        Else
            SetTrueText("Draw a rectangle around any object to be tracked in the BGR image above.", New cvb.Point(10, 140))
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Tracker_Basics_Close(cPtr)
    End Sub
End Class
