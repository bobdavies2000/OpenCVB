Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class EdgeLine_Basics : Inherits TaskParent
    Public classCount As Integer = 2
    Public alwaysFullImage As Boolean ' useful for left/right images where motion is only the left image.
    Public Sub New()
        cPtr = EdgeLineSimple_Open()
        desc = "Retain the existing edge/lines and add the edge/lines where motion occurred."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLineSimple_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, 1)
        handleSrc.Free()
        dst1 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)

        If task.firstPass Or alwaysFullImage Then
            dst2 = dst1.Clone
        Else
            dst1.CopyTo(dst2, task.motionMask)
        End If
        dst2.Rectangle(New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1), 255, 2) ' prevent leaks at the image boundary...
    End Sub
    Public Sub Close()
        EdgeLineSimple_Close(cPtr)
    End Sub
End Class







Public Class EdgeLine_Raw : Inherits TaskParent
    Public segments As New List(Of List(Of cv.Point))
    Public Sub New()
        cPtr = EdgeLineRaw_Open()
        labels = {"", "", "CV_8U Image showing merged lines and edges.", "CV_32S image - lines identified by their index in segments list"}
        desc = "Use EdgeLines to find edges/lines but without using motionMask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLineRaw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst3 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)
        dst3.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)

        Dim segCount = EdgeLine_GetSegCount(cPtr)
        segments.Clear()

        For i = 0 To segCount - 1
            Dim len = EdgeLineRaw_NextLength(cPtr)
            Dim nextSeg(len - 1) As Integer
            Dim segPtr = EdgeLineRaw_NextSegment(cPtr)
            Marshal.Copy(segPtr, nextSeg, 0, nextSeg.Length)

            Dim segment As New List(Of cv.Point)
            For j = 0 To nextSeg.Length - 2 Step 2
                segment.Add(New cv.Point(nextSeg(j), nextSeg(j + 1)))
            Next
            segments.Add(segment)
        Next
    End Sub
    Public Sub Close()
        EdgeLineRaw_Close(cPtr)
    End Sub
End Class






Public Class EdgeLine_JustLines : Inherits TaskParent
    Public Sub New()
        cPtr = EdgeLine_Image_Open()
        labels = {"", "", "EdgeLine_Image output", ""}
        desc = "Access the EdgeDraw algorithm directly rather than through to CPP_Basics interface - more efficient"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLine_Image_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8UC1, imagePtr)
    End Sub
    Public Sub Close()
        EdgeLine_Image_Close(cPtr)
    End Sub
End Class








Public Class EdgeLine_SplitMean : Inherits TaskParent
    Dim binary As New Bin4Way_SplitMean
    Dim edges As New EdgeLine_Basics
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





Public Class EdgeLine_Segments : Inherits TaskParent
    Public segments As New List(Of List(Of cv.Point))
    Public Sub New()
        cPtr = EdgeLineRaw_Open()
        desc = "Get the segments from the EdgeDraw C++ algorithm - the list of points for each line in the output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLineRaw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)

        Dim segCount = EdgeLineRaw_GetSegCount(cPtr)
        segments.Clear()

        For i = 0 To segCount - 1
            Dim len = EdgeLineRaw_NextLength(cPtr)
            Dim nextSeg(len - 1) As Integer
            Dim segPtr = EdgeLineRaw_NextSegment(cPtr)
            Marshal.Copy(segPtr, nextSeg, 0, nextSeg.Length)

            Dim segment As New List(Of cv.Point)
            For j = 0 To nextSeg.Length - 2 Step 2
                segment.Add(New cv.Point(nextSeg(j), nextSeg(j + 1)))
            Next
            segments.Add(segment)
        Next

    End Sub
    Public Sub Close()
        EdgeLineRaw_Close(cPtr)
    End Sub
End Class







Public Class EdgeLine_BasicsMotion : Inherits TaskParent
    Public edgeList As New List(Of List(Of cv.Point))
    Public Sub New()
        cPtr = EdgeLine_Open()
        desc = "Native C++ version to find edges/lines using motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)

        Dim maskData(task.motionMask.Total - 1) As Byte
        Marshal.Copy(task.motionMask.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        Dim imagePtr = EdgeLine_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols,
                                        task.lineWidth)
        handleSrc.Free()
        handleMask.Free()

        dst2 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)
        If task.heartBeat Then
            labels(2) = "There were " + CStr(EdgeLine_GetEdgeLength(cPtr)) + " edge/lines found while " +
                                        CStr(EdgeLine_GetSegCount(cPtr)) + " edge/lines were found on the current image."
            labels(3) = "There were " + CStr(EdgeLine_UnchangedCount(cPtr)) + " edge/lines retained from the previous image."
        End If
    End Sub
    Public Sub Close()
        EdgeLine_Close(cPtr)
    End Sub
End Class






Public Class EdgeLine_Construct : Inherits TaskParent
    Dim edges As New Edge_Basics
    Dim lines As New Line_Basics
    Public classCount = 2
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Construct a combination of lines and edges using Line_Basics and Edge_Basics."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lines.Run(src)
        dst2.SetTo(0)
        For Each lp In lines.lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, cv.LineTypes.Link8)
        Next

        edges.Run(src)
        dst3 = edges.dst2

        dst2 = dst2 Or dst3
    End Sub
End Class
