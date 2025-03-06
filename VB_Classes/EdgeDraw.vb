Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class EdgeDraw_Basics : Inherits TaskParent
    Public Sub New()
        cPtr = EdgeDraw_Edges_Open()
        labels = {"", "", "EdgeDraw_Basics output", ""}
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeDraw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
        dst2.Rectangle(New cv.Rect(0, 0, dst2.Width, dst2.Height), 255, task.lineWidth)
    End Sub
    Public Sub Close()
        EdgeDraw_Edges_Close(cPtr)
    End Sub
End Class







Public Class EdgeDraw_Segments : Inherits TaskParent
    Public segPoints As New List(Of cv.Point2f)
    Public Sub New()
        cPtr = EdgeDraw_Lines_Open()
        labels = {"", "", "EdgeDraw_Segments output", ""}
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim vecPtr = EdgeDraw_Lines_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()

        Dim ptData = cv.Mat.FromPixelData(EdgeDraw_Lines_Count(cPtr), 2, cv.MatType.CV_32FC2, vecPtr).Clone
        dst2.SetTo(0)
        If task.heartBeat Then dst3.SetTo(0)
        segPoints.Clear()
        For i = 0 To ptData.Rows - 1 Step 2
            Dim pt1 = ptData.Get(Of cv.Point2f)(i, 0)
            Dim pt2 = ptData.Get(Of cv.Point2f)(i, 1)
            DrawLine(dst2, pt1, pt2, white)
            dst3 += dst2
            segPoints.Add(pt1)
            segPoints.Add(pt2)
        Next
    End Sub
    Public Sub Close()
        EdgeDraw_Lines_Close(cPtr)
    End Sub
End Class







Public Class EdgeDraw_LeftRight : Inherits TaskParent
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        desc = "Find edges is the left and right images using EdgeDraw..."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        edges.Run(task.leftView)
        dst2 = edges.dst2.Clone

        edges.Run(task.rightView)
        dst3 = edges.dst2
    End Sub
End Class







Public Class EdgeDraw_LeftRightVertical : Inherits TaskParent
    Dim edges As New EdgeDraw_Basics
    Dim vert As New Rotate_Verticalize
    Public Sub New()
        desc = "Find edges is the left and right images using EdgeDraw after verticalizing the images."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        vert.Run(task.leftView)
        edges.Run(vert.dst2)
        dst2 = edges.dst2.Clone

        vert.Run(task.rightView)
        edges.Run(vert.dst2)
        dst3 = edges.dst2
    End Sub
End Class







Public Class EdgeDraw_SplitMean : Inherits TaskParent
    Dim binary As New Bin4Way_SplitMean
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "find the edges in a 4-way color split of the image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
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






Public Class EdgeDraw_GridCell : Inherits TaskParent
    Dim edges As New Edge_Basics
    Public Sub New()
        desc = "Find lines within each grid cell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        For Each idd In task.iddList
            edges.Run(src(idd.cRect))
            For Each lp In task.lpList
                dst2.Line(lp.p1, lp.p2, task.HighlightColor, task.lineWidth)
            Next
        Next
    End Sub
End Class
