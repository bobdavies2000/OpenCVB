Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class EdgeDraw_Basics : Inherits VB_Parent
    Public Sub New()
        cPtr = EdgeDraw_Edges_Open()
        labels = {"", "", "EdgeDraw_Basics output", ""}
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeDraw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
        dst2.Rectangle(New cv.Rect(0, 0, dst2.Width, dst2.Height), 255, task.lineWidth)
    End Sub
    Public Sub Close()
        EdgeDraw_Edges_Close(cPtr)
    End Sub
End Class







Public Class EdgeDraw_Segments : Inherits VB_Parent
    Public segPoints As New List(Of cv.Point2f)
    Public Sub New()
        cPtr = EdgeDraw_Lines_Open()
        labels = {"", "", "EdgeDraw_Segments output", ""}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim vecPtr = EdgeDraw_Lines_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()

        Dim ptData = New cv.Mat(EdgeDraw_Lines_Count(cPtr), 2, cv.MatType.CV_32FC2, vecPtr).Clone
        dst2.SetTo(0)
        If task.heartBeat Then dst3.SetTo(0)
        segPoints.Clear()
        For i = 0 To ptData.Rows - 1 Step 2
            Dim pt1 = ptData.Get(Of cv.Point2f)(i, 0)
            Dim pt2 = ptData.Get(Of cv.Point2f)(i, 1)
            DrawLine(dst2, pt1, pt2, cv.Scalar.White)
            dst3 += dst2
            segPoints.Add(pt1)
            segPoints.Add(pt2)
        Next
    End Sub
    Public Sub Close()
        EdgeDraw_Lines_Close(cPtr)
    End Sub
End Class