Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Guess_Depth_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = Guess_Depth_Open()
        labels = {"", "", "Updated point cloud (holes filled)", "Original point cloud"}
        desc = "Fill single pixel holes in the point cloud."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Guess_Depth_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32FC3, imagePtr).Clone
        If standalone Then dst3 = task.pointCloud
    End Sub
    Public Sub Close() 
        Guess_Depth_Close(cPtr)
    End Sub
End Class

Module Guess_Depth_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Guess_Depth_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Guess_Depth_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Guess_Depth_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32) As IntPtr
    End Function
End Module







Public Class Guess_ImageEdges_CPP : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max Distance from edge", 0, 100, 50)

        cPtr = Guess_ImageEdges_Open()
        labels = {"", "", "Updated point cloud - nearest depth to each edge is replicated to the image boundary", "Original point cloud"}
        desc = "Replicate the nearest depth measurement at all the image edges"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If task.cameraName = "Oak-D camera" Or task.cameraName = "Azure Kinect 4K" Then
            setTrueText("Only RealSense cameras are likely to benefit from enhanced depth at the image edges.")
            Exit Sub
        End If
        Static distSlider = findSlider("Max Distance from edge")

        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Guess_ImageEdges_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, distSlider.value)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_32FC3, cppData).Clone
        If standalone Then dst3 = task.pointCloud
    End Sub
    Public Sub Close()
        Guess_ImageEdges_Close(cPtr)
    End Sub
End Class

Module Guess_ImageEdges_CPP_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Guess_ImageEdges_Open() As IntPtr
    End Function
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Sub Guess_ImageEdges_Close(cPtr As IntPtr)
    End Sub
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Guess_ImageEdges_RunCPP(cPtr As IntPtr, dataPtr As IntPtr, rows As Int32, cols As Int32, maxDistanceToEdge As Int32) As IntPtr
    End Function
End Module