Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Density_Basics : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Distance in meters X10000", 1, 2000, task.densityMetric)
        cPtr = Density_2D_Open()
        desc = "Isolate points in 3D using the distance to the 8 neighboring points in the pointcloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static distSlider = findSlider("Distance in meters X10000")
        Dim distance As Single = distSlider.value / 10000

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Density_2D_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, distance)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
    End Sub
    Public Sub Close()
        Density_2D_Close(cPtr)
    End Sub
End Class





Public Class Density_Phase : Inherits VB_Algorithm
    Dim dense As New Density_Basics
    Dim gradient As New Gradient_Depth
    Public Sub New()
        desc = "Display gradient phase and 2D density side by side."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gradient.Run(Nothing)
        dst3 = vbNormalize32f(gradient.dst3)

        dense.Run(src)
        dst2 = dense.dst2
    End Sub
End Class













Module Density_2D_CPP_Module

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Density_2D_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Density_2D_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Density_2D_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, distance As Single) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Density_Count_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Density_Count_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Density_Count_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, zCount As Integer) As IntPtr
    End Function
End Module







Public Class Density_Count_CPP : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Neighboring Z count", 0, 8, 3)
        cPtr = Density_Count_Open()
        desc = "Isolate points in 3D by counting 8 neighboring Z points in the pointcloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static distSlider = findSlider("Neighboring Z count")
        Dim zCount As Integer = distSlider.value

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Density_Count_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, zCount)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
    End Sub
    Public Sub Close()
        Density_Count_Close(cPtr)
    End Sub
End Class







Public Class Density_Mask : Inherits VB_Algorithm
    Public pointList As New List(Of cv.Point)
    Public Sub New()
        desc = "Measure a mask's size in any image and track the biggest regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        src.SetTo(0, task.noDepthMask)

        Dim threshold = gOptions.GridSize.Value * gOptions.GridSize.Value / 2
        Dim activeList(task.gridList.Count - 1) As Boolean
        dst3.SetTo(0)
        Parallel.For(0, task.gridList.Count,
             Sub(i)
                 Dim roi = task.gridList(i)
                 Dim count = src(roi).CountNonZero
                 If count > threshold Then
                     dst3(roi).SetTo(cv.Scalar.White)
                     activeList(i) = True
                 End If
             End Sub)

        pointList.Clear()

        For i = 0 To activeList.Count - 1
            If activeList(i) Then
                Dim roi = task.gridList(i)
                pointList.Add(New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2))
            End If
        Next
    End Sub
End Class