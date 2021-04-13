Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Module Salience_Exports
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Salience_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Salience_Run(classPtr As IntPtr, numScales As integer, grayInput As IntPtr, rows As integer, cols As integer) As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Salience_Close(classPtr As IntPtr)
    End Sub
End Module




Public Class Salience_Basics_CPP
    Inherits VBparent
    Dim grayData(0) As Byte
    Dim numScales As integer
    Dim salience As IntPtr
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Salience numScales", 1, 6, 6)
        End If

        salience = Salience_Open()
        task.desc = "Show results of Salience algorithm when using C++"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If src.Total <> grayData.Length Then ReDim grayData(src.Total - 1)
        Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
        Marshal.Copy(src.Data, grayData, 0, grayData.Length)
        Dim imagePtr = Salience_Run(salience, sliders.trackbar(0).Value, grayHandle.AddrOfPinnedObject, src.Height, src.Width)
        grayHandle.Free()

        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)
    End Sub
    Public Sub Close()
        Salience_Close(salience)
    End Sub
End Class



Public Class Salience_Basics_MT
    Inherits VBparent
    Dim salience As Salience_Basics_CPP
    Public Sub New()
        initParent()
        salience = New Salience_Basics_CPP()
        salience.sliders.trackbar(1).Value = 2

        task.desc = "Show results of multi-threaded Salience algorithm when using C++.  NOTE: salience is relative."
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim numScales = salience.sliders.trackbar(0).Value
        Dim threads = salience.sliders.trackbar(1).Value
        Dim h = CInt(src.Height / threads)
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U)
        Parallel.For(0, threads,
            Sub(i)
                Dim roi = New cv.Rect(0, i * h, src.Width, Math.Min(h, src.Height - i * h))
                If roi.Height <= 0 Then Exit Sub

                Dim salience = Salience_Open()
                Dim input = src(roi).Clone()
                Dim grayData(input.Total - 1) As Byte
                Dim grayHandle = GCHandle.Alloc(grayData, GCHandleType.Pinned)
                Marshal.Copy(input.Data, grayData, 0, grayData.Length)
                Dim imagePtr = Salience_Run(salience, numScales, grayHandle.AddrOfPinnedObject, roi.Height, roi.Width)
                grayHandle.Free()

                Dim tmp As New cv.Mat(roi.Height, roi.Width, cv.MatType.CV_8U, imagePtr)
                dst1(roi) = tmp
                Salience_Close(salience)
            End Sub)
    End Sub
End Class



