Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
'Public Class Tracker_Basics
'    Inherits VBparent
'    Dim tracker As Object
'    Public trackerCSRT As cv.Tracking.TrackerCSRT
'    Public trackerGOTURN As cv.TrackerGOTURN
'    Public trackerMIL As cv.TrackerMIL
'    Public trackerKCF As cv.Tracking.TrackerKCF
'    Public bbox As cv.Rect2d
'    Public boxObject() As cv.Rect2d
'    Public trackerIndex As Integer
'    Public Sub New()
'        initParent()

'        If findfrm(caller + " Radio Options") Is Nothing Then
'            radio.Setup(caller, 8)
'            radio.check(0).Text = "TrackerCSRT - Channel and Spatial Reliability Tracker"
'            radio.check(1).Text = "TrackerGOTURN"
'            radio.check(2).Text = "TrackerMIL"
'            radio.check(3).Text = "TrackerKCF"
'            radio.check(0).Checked = True ' TrackerMIL is the default
'        End If

'        If findfrm(caller + " CheckBox Options") Is Nothing Then
'            check.Setup(caller, 1)
'            check.Box(0).Text = "Stop tracking selected object"
'        End If

'        task.desc = "Track an object using cv.Tracking API's - tracker algorithm"
'    End Sub
'    Public Sub Run(src as cv.Mat)

'        Dim input = src
'        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

'        task.trueText("Draw a rectangle around object to be tracked.", 10, 140, 3)
'        If check.Box(0).Checked Then
'            check.Box(0).Checked = False
'            If tracker IsNot Nothing Then tracker.Dispose()
'            tracker = Nothing
'        End If

'        Static frm = findfrm(caller + " Radio Options")
'        For i = 0 To frm.check.length - 1
'            If frm.check(i).Checked = True Then
'                label1 = "Method: " + radio.check(i).Text
'                trackerIndex = i
'                Exit For
'            End If
'        Next

'        If task.drawRect.Width <> 0 Then
'            Dim mask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
'            mask.Rectangle(task.drawRect, cv.Scalar.White, -1)
'            Select Case trackerIndex
'                Case 0
'                    trackerCSRT = cv.Tracking.TrackerCSRT.Create()
'                    trackerCSRT.Init(input, task.drawRect)
'                    trackerCSRT.SetInitialMask(mask)

'                    tracker = trackerCSRT
'                    'Case 1
'                    '    trackerGOTURN.Add(cv.TrackerGOTURN.Create, src, bbox)
'                    'Case 2
'                    '    trackerMIL.Add(cv.TrackerMIL.Create, src, bbox)
'                    'Case 3
'                    '    tracker.Add(cv.Tracking.TrackerKCF.Create, src, bbox)
'            End Select
'        End If

'        dst1 = input.Clone()
'        If tracker IsNot Nothing Then
'            trackerCSRT.Update(input, task.drawRect)
'            dst1.Rectangle(task.drawRect, cv.Scalar.Yellow, 2)
'        End If

'    End Sub
'End Class





'Public Class Tracker_MultiObject
'    Inherits VBparent
'    Dim trackers As New List(Of Tracker_Basics)
'    Public Sub New()
'        initParent()
'        task.desc = "Track any number of objects simultaneously - tracker algorithm"
'    End Sub
'    Public Sub Run(src as cv.Mat)
'        If task.drawRect.Width <> 0 Then
'            Dim tr = New Tracker_Basics()
'            tr.src = src
'            tr.Run()
'            task.drawRect = New cv.Rect
'            trackers.Add(tr)
'        End If
'        dst1 = src.Clone()
'        For Each tr In trackers
'            Dim closeIt As Boolean
'            If tr.check.Box(0).Checked Then closeIt = True
'            tr.src = src
'            tr.Run()
'            If closeIt Then tr.check.Dispose()
'            If tr.tracker IsNot Nothing Then
'                Dim p1 = New cv.Point(tr.boxObject(0).X, tr.boxObject(0).Y)
'                Dim p2 = New cv.Point(tr.boxObject(0).X + tr.bbox.Width, tr.boxObject(0).Y + tr.bbox.Height)
'                dst1.Rectangle(p1, p2, cv.Scalar.Blue, 2)
'            End If
'        Next
'    End Sub
'End Class









Module Tracker_Basics_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Tracker_Basics_Open(trackType As Integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Tracker_Basics_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Tracker_Basics_Run(cPtr As IntPtr, rgbPtr As IntPtr, rows As Int32, cols As Int32, channels As Int32,
                                       x As Integer, y As Integer, w As Integer, h As Integer) As IntPtr
    End Function
End Module



' https://learnopencv.com/object-tracking-using-opencv-cpp-python/
Public Class Tracker_Basics
    Inherits VBparent
    Dim cPtr As IntPtr
    Dim trackType As Integer
    Public Sub New()
        initParent()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 8)
            radio.check(0).Text = "Boosting"
            radio.check(1).Text = "MIL"
            radio.check(2).Text = "KCF"
            radio.check(3).Text = "TLD"
            radio.check(4).Text = "MedianFlow"
            radio.check(5).Text = "GoTurn"
            radio.check(6).Text = "Mosse"
            radio.check(7).Text = "TrackerCSRT - Channel and Spatial Reliability Tracker"
            radio.check(7).Checked = True ' TrackerMIL is the default
        End If

        task.desc = "Use C++ to track objects"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)


        Dim input = src
        If input.Channels <> 1 Then input = input.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

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

        If task.drawRect.Width <> 0 Then
            Dim srcData(input.Total * input.ElemSize) As Byte
            Marshal.Copy(input.Data, srcData, 0, srcData.Length - 1)
            Dim handleSrc = GCHandle.Alloc(srcData, GCHandleType.Pinned)
            Dim r = task.drawRect
            Dim imagePtr = Tracker_Basics_Run(cPtr, handleSrc.AddrOfPinnedObject(), input.Rows, input.Cols, input.Channels, r.X, r.Y, r.Width, r.Height)
            handleSrc.Free()

            If imagePtr <> 0 Then
                Dim dstData(input.Total * input.ElemSize - 1) As Byte
                Marshal.Copy(imagePtr, dstData, 0, dstData.Length)
                dst1 = New cv.Mat(input.Rows, input.Cols, If(input.Channels = 3, cv.MatType.CV_8UC3, cv.MatType.CV_8UC1), dstData)
            End If
        Else
            task.trueText("Draw a rectangle around any object to be tracked in the RGB image above.", 10, 140)
        End If
    End Sub
    Public Sub Close()
        Tracker_Basics_Close(cPtr)
    End Sub
End Class

