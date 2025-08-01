Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Density_Basics : Inherits TaskParent
    Dim options = New Options_Density
    Public Sub New()
        cPtr = Density_2D_Open()
        desc = "Isolate points in 3D using the distance to the 8 neighboring points in the pointcloud"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Density_2D_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.distance)
        handleSrc.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
    End Sub
    Public Sub Close()
        Density_2D_Close(cPtr)
    End Sub
End Class





Public Class Density_Phase : Inherits TaskParent
    Dim dense As New Density_Basics
    Dim gradient As New Gradient_PhaseDepth
    Public Sub New()
        desc = "Display gradient phase and 2D density side by side."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        gradient.Run(src)
        dst3 = Convert32f_To_8UC3(gradient.dst3)

        dense.Run(src)
        dst2 = dense.dst2
    End Sub
End Class







Public Class Density_Count_CPP : Inherits TaskParent
    Dim options = New Options_Density
    Public Sub New()
        cPtr = Density_Count_Open()
        desc = "Isolate points in 3D by counting 8 neighboring Z points in the pointcloud"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Density_Count_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.zCount)
        handleSrc.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
    End Sub
    Public Sub Close()
        Density_Count_Close(cPtr)
    End Sub
End Class







Public Class Density_Mask : Inherits TaskParent
    Public pointList As New List(Of cv.Point)
    Public Sub New()
        desc = "Measure a mask's size in any image and track the biggest regions."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = task.gray
        src.SetTo(0, task.noDepthMask)

        Dim threshold = task.brickSize * task.brickSize / 2
        Dim activeList(task.gridRects.Count - 1) As Boolean
        dst3.SetTo(0)
        Parallel.For(0, task.gridRects.Count,
             Sub(i)
                 Dim roi = task.gridRects(i)
                 Dim count = src(roi).CountNonZero
                 If count > threshold Then
                     dst3(roi).SetTo(white)
                     activeList(i) = True
                 End If
             End Sub)

        pointList.Clear()

        For i = 0 To activeList.Count - 1
            If activeList(i) Then
                Dim roi = task.gridRects(i)
                pointList.Add(New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2))
            End If
        Next
    End Sub
End Class