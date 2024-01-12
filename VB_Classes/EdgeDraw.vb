Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class EdgeDraw_Basics : Inherits VB_Algorithm
    Public Sub New()
        cPtr = EdgeDraw_Edges_Open()
        labels = {"", "", "EdgeDraw_Basics output", ""}
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
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







Public Class EdgeDraw_Segments : Inherits VB_Algorithm
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
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim vecPtr = EdgeDraw_Lines_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()

        Dim ptData = New cv.Mat(EdgeDraw_Lines_Count(cPtr), 2, cv.MatType.CV_32FC2, vecPtr).Clone
        dst2.SetTo(0)
        If heartBeat() Then dst3.SetTo(0)
        segPoints.Clear()
        For i = 0 To ptData.Rows - 1 Step 2
            Dim pt1 = ptData.Get(Of cv.Point2f)(i, 0)
            Dim pt2 = ptData.Get(Of cv.Point2f)(i, 1)
            dst2.Line(pt1, pt2, cv.Scalar.White, task.lineWidth, task.lineType)
            dst3 += dst2
            segPoints.Add(pt1)
            segPoints.Add(pt2)
        Next
    End Sub
    Public Sub Close()
        EdgeDraw_Lines_Close(cPtr)
    End Sub
End Class








'Public Class EdgeDraw_LineData : Inherits VB_Algorithm
'    Dim edgeD As New EdgeDraw
'    Dim knn As New KNN_Basics
'    Public pointPairLists As New List(Of List(Of cv.Point2f))
'    Public Sub New()
'        knn.desiredMatches = 2
'        labels = {"", "", "Before KNN", "After KNN "}
'        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
'        desc = "Use KNN to track EdgeDraw_Basics lines across frames"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        If task.optionsChanged Then pointPairLists.Clear()

'        edgeD.Run(src)
'        dst2 = edgeD.dst2

'        knn.trainInput = knn.queries

'        knn.queries.Clear()
'        For Each pt In edgeD.segPoints
'            knn.queries.Add(pt)
'        Next

'        knn.Run(empty)
'        If knn.neighbors Is Nothing Then Exit Sub ' no query points?
'        If knn.neighbors.Count < 2 Then Exit Sub ' not enough points.
'        If edgeD.segPoints.Count = 0 Then Exit Sub

'        Dim matches As New List(Of cv.Point2f)
'        Dim maxDistance = gOptions.PixelDiffThreshold.Value
'        For i = 0 To knn.neighbors.Count - 2 Step 2
'            Dim index = knn.neighbors(i)(1)
'            Dim distance = edgeD.segPoints(i).DistanceTo(edgeD.segPoints(index))
'            If distance <= maxDistance Then
'                Dim p1 = edgeD.segPoints(index)
'                index = knn.neighbors(i + 1)(1)
'                distance = edgeD.segPoints(i + 1).DistanceTo(edgeD.segPoints(index))
'                If distance <= maxDistance Then
'                    matches.Add(p1)
'                    matches.Add(edgeD.segPoints(index))
'                End If
'            End If
'        Next

'        dst3.SetTo(0)
'        For Each points In pointPairLists
'            For i = 0 To points.Count - 2 Step 2
'                Dim p1 = points(i)
'                Dim p2 = points(i + 1)
'                dst3.Line(p1, p2, cv.Scalar.White, task.lineWidth + 3, task.lineType)
'            Next
'        Next

'        pointPairLists.Add(matches)
'        If pointPairLists.Count >= task.frameHistoryCount Then pointPairLists.RemoveAt(0)

'        labels(2) = CStr(edgeD.segPoints.Count / 2) + " lines were found and " + CStr(matches.Count / 2) + " matched the previous generation."
'    End Sub
'End Class