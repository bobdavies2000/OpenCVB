Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Density_Basics : Inherits VB_Parent
    Dim options = New Options_Density
    Public Sub New()
        cPtr = Density_2D_Open()
        UpdateAdvice(traceName + ": use local options to control separation of points in 3D.")
        desc = "Isolate points in 3D using the distance to the 8 neighboring points in the pointcloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Density_2D_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.distance)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
    End Sub
    Public Sub Close()
        Density_2D_Close(cPtr)
    End Sub
End Class





Public Class Density_Phase : Inherits VB_Parent
    Dim dense As New Density_Basics
    Dim gradient As New Gradient_Depth
    Public Sub New()
        desc = "Display gradient phase and 2D density side by side."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        gradient.Run(empty)
        dst3 = GetNormalize32f(gradient.dst3)

        dense.Run(src)
        dst2 = dense.dst2
    End Sub
End Class







Public Class Density_Count_CPP_VB : Inherits VB_Parent
    Dim options = New Options_Density
    Public Sub New()
        cPtr = Density_Count_Open()
        desc = "Isolate points in 3D by counting 8 neighboring Z points in the pointcloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)

        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = Density_Count_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.zCount)
        handleSrc.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
    End Sub
    Public Sub Close()
        Density_Count_Close(cPtr)
    End Sub
End Class







Public Class Density_Mask : Inherits VB_Parent
    Public pointList As New List(Of cv.Point)
    Public Sub New()
        desc = "Measure a mask's size in any image and track the biggest regions."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        src.SetTo(0, task.noDepthMask)

        Dim threshold = task.gridSize * task.gridSize / 2
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