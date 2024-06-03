Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://learnopencv.com/object-tracking-using-opencv-cpp-python/
Public Class Tracker_Basics : Inherits VB_Parent
    Public tRect As cv.Rect
    Public Sub New()
        If radio.Setup(traceName) Then
            radio.addRadio("Boosting")
            radio.addRadio("MIL")
            radio.addRadio("KCF")
            radio.addRadio("TLD")
            radio.addRadio("MedianFlow")
            radio.addRadio("GoTurn")
            radio.addRadio("Mosse")
            radio.addRadio("TrackerCSRT - Channel and Spatial Reliability Tracker")
            radio.check(1).Checked = True
        End If

        If task.testAllRunning Then task.drawRect = New cv.Rect(25, 25, 25, 25)
        desc = "Use C++ to track objects.  Results are poor compared to Match_DrawRect"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static frm = findfrm(traceName + " Radio Buttons")
        Static trackType As Integer
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked = True Then
                labels(2) = "Method: " + radio.check(i).Text
                trackType = i
                Exit For
            End If
        Next

        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static saveRect As New cv.Rect
        Static saveTrackerType = -1
        If task.drawRect <> saveRect Or saveTrackerType <> trackType Then
            If cPtr <> 0 Then Tracker_Basics_Close(cPtr)
            cPtr = Tracker_Basics_Open(trackType)
            saveTrackerType = trackType
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
            dst2.Rectangle(tRect, cv.Scalar.White, task.lineWidth)
        Else
            setTrueText("Draw a rectangle around any object to be tracked in the BGR image above.", New cv.Point(10, 140))
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Tracker_Basics_Close(cPtr)
    End Sub
End Class
