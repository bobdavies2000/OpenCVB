Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
' https://learnopencv.com/object-tracking-using-opencv-cpp-python/
Public Class Tracker_Basics : Inherits VBparent
    Dim cPtr As IntPtr
    Dim trackType As Integer
    Public Sub New()
        If radio.Setup(caller, 8) Then
            radio.check(0).Text = "Boosting"
            radio.check(1).Text = "MIL"
            radio.check(2).Text = "KCF"
            radio.check(3).Text = "TLD"
            radio.check(4).Text = "MedianFlow"
            radio.check(5).Text = "GoTurn"
            radio.check(6).Text = "Mosse"
            radio.check(7).Text = "TrackerCSRT - Channel and Spatial Reliability Tracker"
            radio.check(1).Checked = True
        End If

        task.drawRect = New cv.Rect(100, 100, 100, 100)
        task.desc = "Use C++ to track objects"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        If task.drawRect.Width <> 0 Then
            Static saveTrackerType = -1
            If saveTrackerType <> trackType Then
                Static frm = findfrm(caller + " Radio Options")
                For i = 0 To frm.check.length - 1
                    If frm.check(i).Checked = True Then
                        label1 = "Method: " + radio.check(i).Text
                        trackType = i
                        Exit For
                    End If
                Next
                If saveTrackerType < 0 Then
                    cPtr = Tracker_Basics_Open(trackType)
                Else
                    Tracker_Basics_Close(cPtr)
                    cPtr = Tracker_Basics_Open(trackType)
                End If
                saveTrackerType = trackType
            End If

            Dim srcData(src.Total * src.ElemSize) As Byte
            Marshal.Copy(src.Data, srcData, 0, srcData.Length - 1)
            Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
            Dim r = task.drawRect
            Dim imagePtr = Tracker_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, r.X, r.Y, r.Width, r.Height)
            handleSrc.Free()

            If imagePtr <> 0 Then
                Dim dstData(src.Total * src.ElemSize - 1) As Byte
                Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
                dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, dstData)
            End If
        Else
            setTrueText("Draw a rectangle around any object to be tracked in the RGB image above.", 10, 140)
        End If
    End Sub
    Public Sub Close()
        Tracker_Basics_Close(cPtr)
    End Sub
End Class





Module Tracker_Basics_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Tracker_Basics_Open(trackType As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Tracker_Basics_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Tracker_Basics_Run(cPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32,
                                       x As Integer, y As Integer, w As Integer, h As Integer) As IntPtr
    End Function
End Module