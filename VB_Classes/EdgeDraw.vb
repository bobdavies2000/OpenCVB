Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp
Public Class EdgeDraw_Basics : Inherits VB_Parent
    Public Sub New()
        cPtr = EdgeDraw_Edges_Open()
        labels = {"", "", "EdgeDraw_Basics output", ""}
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeDraw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst2 = cvb.Mat.FromPixelData(src.Rows, src.Cols, cvb.MatType.CV_8UC1, imagePtr)
        dst2.Rectangle(New cvb.Rect(0, 0, dst2.Width, dst2.Height), 255, task.lineWidth)
    End Sub
    Public Sub Close()
        EdgeDraw_Edges_Close(cPtr)
    End Sub
End Class







Public Class EdgeDraw_Segments : Inherits VB_Parent
    Public segPoints As New List(Of cvb.Point2f)
    Public Sub New()
        cPtr = EdgeDraw_Lines_Open()
        labels = {"", "", "EdgeDraw_Segments output", ""}
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        dst3 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim vecPtr = EdgeDraw_Lines_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()

        Dim ptData = cvb.Mat.FromPixelData(EdgeDraw_Lines_Count(cPtr), 2, cvb.MatType.CV_32FC2, vecPtr).Clone
        dst2.SetTo(0)
        If task.heartBeat Then dst3.SetTo(0)
        segPoints.Clear()
        For i = 0 To ptData.Rows - 1 Step 2
            Dim pt1 = ptData.Get(Of cvb.Point2f)(i, 0)
            Dim pt2 = ptData.Get(Of cvb.Point2f)(i, 1)
            DrawLine(dst2, pt1, pt2, cvb.Scalar.White)
            dst3 += dst2
            segPoints.Add(pt1)
            segPoints.Add(pt2)
        Next
    End Sub
    Public Sub Close()
        EdgeDraw_Lines_Close(cPtr)
    End Sub
End Class







Public Class EdgeDraw_LeftRight : Inherits VB_Parent
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        desc = "Find edges is the left and right images using EdgeDraw..."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        edges.Run(task.leftView)
        dst2 = edges.dst2.Clone

        edges.Run(task.rightView)
        dst3 = edges.dst2
    End Sub
End Class







Public Class EdgeDraw_LeftRightVertical : Inherits VB_Parent
    Dim edges As New EdgeDraw_Basics
    Dim vert As New Rotate_Verticalize
    Public Sub New()
        desc = "Find edges is the left and right images using EdgeDraw after verticalizing the images."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        vert.Run(task.leftView)
        edges.Run(vert.dst2)
        dst2 = edges.dst2.Clone

        vert.Run(task.rightView)
        edges.Run(vert.dst2)
        dst3 = edges.dst2
    End Sub
End Class







Public Class EdgeDraw_SplitMean : Inherits VB_Parent
    Dim binary As New Bin4Way_SplitMean
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size, cvb.MatType.CV_8U, 0)
        desc = "find the edges in a 4-way color split of the image."
    End Sub
    Public Sub RunVB(src As cvb.Mat)
        binary.Run(src)

        dst2.SetTo(0)
        For i = 0 To binary.mats.mat.Count - 1
            edges.Run(binary.mats.mat(i))
            dst2 = dst2 Or edges.dst2
        Next
        edges.Run(src)
        dst3 = edges.dst2
    End Sub
End Class
