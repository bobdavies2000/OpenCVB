Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp
Public Class EdgeLines_Image : Inherits TaskParent
    Public Sub New()
        cPtr = EdgeLines_Image_Open()
        labels = {"", "", "EdgeLines_Image output", ""}
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLines_Image_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
    End Sub
    Public Sub Close()
        EdgeLines_Image_Close(cPtr)
    End Sub
End Class







Public Class EdgeLines_Lines : Inherits TaskParent
    Public segPoints As New List(Of cv.Point2f)
    Public Sub New()
        cPtr = EdgeLines_Lines_Open()
        labels = {"", "", "EdgeLines_Segments output", ""}
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim vecPtr = EdgeLines_Lines_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()

        Dim ptData = cv.Mat.FromPixelData(EdgeLines_Lines_Count(cPtr), 2, cv.MatType.CV_32FC2, vecPtr).Clone
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
        EdgeLines_Lines_Close(cPtr)
    End Sub
End Class







Public Class EdgeLines_LeftRight : Inherits TaskParent
    Dim edges As New EdgeLines_Image
    Public Sub New()
        desc = "Find edges is the left and right images using EdgeDraw..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.leftView)
        dst2 = edges.dst2.Clone

        edges.Run(task.rightView)
        dst3 = edges.dst2
    End Sub
End Class







Public Class EdgeLines_LeftRightVertical : Inherits TaskParent
    Dim edges As New EdgeLines_Image
    Dim vert As New Rotate_Verticalize
    Public Sub New()
        desc = "Find edges is the left and right images using EdgeDraw after verticalizing the images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        vert.Run(task.leftView)
        edges.Run(vert.dst2)
        dst2 = edges.dst2.Clone

        vert.Run(task.rightView)
        edges.Run(vert.dst2)
        dst3 = edges.dst2
    End Sub
End Class







Public Class EdgeLines_SplitMean : Inherits TaskParent
    Dim binary As New Bin4Way_SplitMean
    Dim edges As New EdgeLines_Image
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "find the edges in a 4-way color split of the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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






Public Class EdgeLines_GridCell : Inherits TaskParent
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




Public Class EdgeLines_Segments : Inherits TaskParent
    Public segments As New List(Of List(Of cv.Point))
    Public Sub New()
        cPtr = EdgeLines_Open()
        desc = "Get the segments from the EdgeDraw C++ algorithm - the list of points for each line in the output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLines_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)

        Dim segCount = EdgeLines_GetLength(cPtr)
        segments.Clear()

        For i = 0 To segCount - 1
            Dim len = EdgeLines_NextLength(cPtr)
            Dim nextSeg(len - 1) As Integer
            Dim segPtr = EdgeLines_NextSegment(cPtr)
            Marshal.Copy(segPtr, nextSeg, 0, nextSeg.Length)

            Dim segment As New List(Of cv.Point)
            For j = 0 To nextSeg.Length - 2 Step 2
                segment.Add(New cv.Point(nextSeg(j), nextSeg(j + 1)))
            Next
            segments.Add(segment)
        Next

    End Sub
    Public Sub Close()
        EdgeLines_Close(cPtr)
    End Sub
End Class





Public Class EdgeLines_Basics : Inherits TaskParent
    Public edgeList As New List(Of List(Of cv.Point))
    Dim eSeg As New EdgeLines_BasicsOld
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Retain edges where there was no motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim histogram As New cv.Mat
        Dim lastList As New List(Of List(Of cv.Point))(edgeList)
        Dim histarray(lastList.Count - 1) As Single

        edgeList.Clear()
        edgeList.Add(New List(Of cv.Point)) ' placeholder for zero
        If lastList.Count > 0 Then
            dst1.SetTo(0, Not task.motionMask)
            Dim ranges1 = New cv.Rangef() {New cv.Rangef(0, lastList.Count)}
            cv.Cv2.CalcHist({dst1}, {0}, emptyMat, histogram, 1, {lastList.Count}, ranges1)
            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

            For i = 1 To histarray.Count - 1
                If histarray(i) = 0 Then edgeList.Add(lastList(i))
            Next
        End If

        eSeg.Run(src)
        ' dst3 = ShowPalette(eSeg.dst2 * 255 / eSeg.segments.Count)

        ReDim histarray(eSeg.segments.Count - 1)

        eSeg.dst2.ConvertTo(dst1, cv.MatType.CV_32F)
        dst1.SetTo(0, Not task.motionMask)

        Dim ranges2 = New cv.Rangef() {New cv.Rangef(0, eSeg.segments.Count)}
        cv.Cv2.CalcHist({dst1}, {0}, emptyMat, histogram, 1, {eSeg.segments.Count}, ranges2)
        Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

        For i = 1 To histarray.Count - 1
            If histarray(i) > 0 Then edgeList.Add(eSeg.segments(i - 1))
        Next

        dst1.SetTo(0)
        dst2.SetTo(0)
        For i = 1 To edgeList.Count - 1
            Dim nextList = New List(Of List(Of cv.Point))
            nextList.Add(edgeList(i))
            Dim n = edgeList(i).Count
            Dim distance As Double = Math.Sqrt((edgeList(i)(0).X - edgeList(i)(n - 1).X) * (edgeList(i)(0).X - edgeList(i)(n - 1).X) +
                                               (edgeList(i)(0).Y - edgeList(i)(n - 1).Y) * (edgeList(i)(0).Y - edgeList(i)(n - 1).Y))
            Dim drawClosed = distance < 10
            cv.Cv2.Polylines(dst1, nextList, drawClosed, i, task.lineWidth, cv.LineTypes.Link4)
            cv.Cv2.Polylines(dst2, nextList, drawClosed, cv.Scalar.White, task.lineWidth, task.lineType)
        Next
        dst3 = ShowPalette(dst1 * 255 / eSeg.segments.Count)

        If task.heartBeat Then
            labels(2) = CStr(eSeg.segments.Count) + " lines found in EdgeLines C++ in the latest image and " +
                        CStr(edgeList.Count) + " resulted after filtering with the motion mask."
            labels(3) = "The " + CStr(eSeg.segments.Count) + " segments found in the current image are colored with the index of each segment"
        End If
    End Sub
End Class




Public Class EdgeLines_BasicsOld : Inherits TaskParent
    Public segments As New List(Of List(Of cv.Point))
    Public Sub New()
        cPtr = EdgeLines_Open()
        labels = {"", "", "EdgeDraw_Basics output", ""}
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLines_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)
        dst2.ConvertTo(dst3, cv.MatType.CV_8U)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)

        Dim segCount = EdgeLines_GetLength(cPtr)
        segments.Clear()

        For i = 0 To segCount - 1
            Dim len = EdgeLines_NextLength(cPtr)
            Dim nextSeg(len - 1) As Integer
            Dim segPtr = EdgeLines_NextSegment(cPtr)
            Marshal.Copy(segPtr, nextSeg, 0, nextSeg.Length)

            Dim segment As New List(Of cv.Point)
            For j = 0 To nextSeg.Length - 2 Step 2
                segment.Add(New cv.Point(nextSeg(j), nextSeg(j + 1)))
            Next
            segments.Add(segment)
        Next

    End Sub
    Public Sub Close()
        EdgeLines_Close(cPtr)
    End Sub
End Class