Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class EdgeLine_Basics : Inherits TaskParent
    Public segments As New List(Of List(Of cv.Point))
    Public rectList As New List(Of cv.Rect)
    Public classCount As Integer
    Public Sub New()
        cPtr = EdgeLineRaw_Open()
        task.gOptions.DebugSlider.Value = 1
        labels(3) = "Highlighting the individual segments one by one."
        desc = "Use EdgeLines to find edges/lines but without using motionMask"
    End Sub
    Public Shared Sub showSegment(dst As cv.Mat)
        If task.quarterBeat Then
            Static debugSegment = 0
            debugSegment += 1
            If debugSegment >= task.edges.segments.Count Then
                debugSegment = 0
                dst.SetTo(0)
            End If
            If debugSegment >= task.edges.segments.Count Then debugSegment = 0
            If debugSegment Then
                    task.edges.dst1 = task.edges.dst2.InRange(debugSegment, debugSegment)
                    task.edges.dst1.CopyTo(dst, task.edges.dst1)
                End If
                debugSegment += 1
            End If
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLineRaw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst1 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, EdgeLineRaw_Rects(cPtr))

        classCount = EdgeLineRaw_GetSegCount(cPtr)
        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)

        rectList.Clear()
        For i = 0 To classCount * 4 - 4 Step 4
            rectList.Add(New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3)))
        Next

        segments.Clear()
        Dim pointCount As Integer
        For i = 0 To classCount - 1
            Dim len = EdgeLineRaw_NextLength(cPtr)
            If len < 2 Then Continue For
            Dim nextSeg(len * 2 - 1) As Integer
            Dim segPtr = EdgeLineRaw_NextSegment(cPtr)
            Marshal.Copy(segPtr, nextSeg, 0, nextSeg.Length)

            Dim segment As New List(Of cv.Point)
            For j = 0 To nextSeg.Length - 2 Step 2
                segment.Add(New cv.Point(nextSeg(j), nextSeg(j + 1)))
                pointCount += 1
            Next
            segments.Add(segment)
        Next
        labels(2) = CStr(classCount) + " segments were found using " + CStr(pointCount) + " points."

        Dim index = Math.Abs(task.gOptions.DebugSlider.Value)
        If index <> 0 Then
            dst3 = task.edges.dst2.InRange(index, index)
            Dim rect = rectList(index - 1)
            dst3.Rectangle(rect, 255, task.lineWidth)
        End If

        'If standaloneTest() Then showSegment(dst3)
    End Sub
    Public Sub Close()
        EdgeLineRaw_Close(cPtr)
    End Sub
End Class







Public Class EdgeLine_Simple : Inherits TaskParent
    Public classCount As Integer
    Public Sub New()
        cPtr = EdgeLineSimple_Open()
        desc = "Retain the existing edge/lines and add the edge/lines where motion occurred."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim input = If(src.Channels() = 1, src.Clone, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        Dim imageEdgeWidth = 2
        If dst2.Width >= 1280 Then imageEdgeWidth = 4

        Dim cppData(input.Total - 1) As Byte
        Marshal.Copy(input.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLineSimple_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), input.Rows, input.Cols, task.lineWidth * 2)
        handleSrc.Free()

        dst2 = cv.Mat.FromPixelData(input.Rows, input.Cols, cv.MatType.CV_8U, imagePtr).Clone
        Dim mm = GetMinMax(task.edges.dst2)
        classCount = mm.maxVal
        dst2.Rectangle(New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1), 255, imageEdgeWidth) ' prevent leaks at the image boundary...
    End Sub
    Public Sub Close()
        EdgeLineSimple_Close(cPtr)
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







Public Class EdgeLine_SimpleMotion : Inherits TaskParent
    Public edgeList As New List(Of List(Of cv.Point))
    Public Sub New()
        cPtr = EdgeLine_Open()
        desc = "Native C++ version to find edges/lines using motion."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim input = If(src.Channels() = 1, src.Clone, task.grayStable.Clone)

        Dim cppData(input.Total - 1) As Byte
        Marshal.Copy(input.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)

        Dim maskData(task.motionMask.Total - 1) As Byte
        Marshal.Copy(task.motionMask.Data, maskData, 0, maskData.Length)
        Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

        Dim imagePtr = EdgeLine_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), input.Rows, input.Cols,
                                        task.lineWidth)
        handleSrc.Free()
        handleMask.Free()

        dst2 = cv.Mat.FromPixelData(input.Rows, input.Cols, cv.MatType.CV_8U, imagePtr)
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








Public Class EdgeLine_LeftRight : Inherits TaskParent
    Dim edges As New EdgeLine_Basics
    Public Sub New()
        labels(3) = "Right View: Note it is updated on every frame - it does not use the motion mask."
        desc = "Build the left and right edge lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        edges.Run(task.leftView)
        dst2 = task.edges.dst2.Clone

        edges.Run(task.rightView)
        dst3 = edges.dst2.Clone
    End Sub
End Class
