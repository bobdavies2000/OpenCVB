Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Guess_Depth_CPP : Inherits TaskParent
    Public Sub New()
        cPtr = Guess_Depth_Open()
        labels = {"", "", "Updated point cloud (holes filled)", "Original point cloud"}
        desc = "Fill single pixel holes in the point cloud."
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Guess_Depth_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32FC3, imagePtr).Clone
        If standaloneTest() Then dst3 = task.pointCloud
    End Sub
    Public Sub Close()
        Guess_Depth_Close(cPtr)
    End Sub
End Class







Public Class Guess_ImageEdges_CPP : Inherits TaskParent
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max Distance from edge", 0, 100, 50)

        cPtr = Guess_ImageEdges_Open()
        labels = {"", "", "Updated point cloud - nearest depth to each edge is replicated to the image boundary", "Original point cloud"}
        desc = "Replicate the nearest depth measurement at all the image edges"
    End Sub
    Public Overrides sub runAlg(src As cv.Mat)
        If task.cameraName = "Oak-D camera" Or task.cameraName = "Azure Kinect 4K" Then
            SetTrueText("Only RealSense cameras are likely to benefit from enhanced depth at the image edges.")
            Exit Sub
        End If
        Static distSlider =optiBase.findslider("Max Distance from edge")

        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Guess_ImageEdges_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, distSlider.value)
        handleSrc.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32FC3, cppData).Clone
        If standaloneTest() Then dst3 = task.pointCloud
    End Sub
    Public Sub Close()
        Guess_ImageEdges_Close(cPtr)
    End Sub
End Class