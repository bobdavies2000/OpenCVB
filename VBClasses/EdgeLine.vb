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
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim cppData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLineRaw_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, task.lineWidth)
        handleSrc.Free()
        If imagePtr <> 0 Then dst1 = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)

        Dim imageEdgeWidth = 2
        If dst2.Width >= 1280 Then imageEdgeWidth = 4
        dst2.Rectangle(New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1), 255, imageEdgeWidth) ' prevent leaks at the image boundary...

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, EdgeLineRaw_Rects(cPtr))

        classCount = EdgeLineRaw_GetSegCount(cPtr)
        If classCount = 0 Then Exit Sub ' nothing to work with....
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

        dst3 = ShowPaletteNoZero(dst2)
    End Sub
    Public Sub Close()
        EdgeLineRaw_Close(cPtr)
    End Sub
End Class






Public Class EdgeLine_BasicsList : Inherits TaskParent
    Public nrclist As New List(Of nrcData)
    Public Sub New()
        task.gOptions.DebugSlider.Value = 1
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
        desc = "Create an entry for each segment"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2
        labels(2) = task.edges.labels(2)

        Dim sortList As New SortedList(Of Integer, nrcData)(New compareAllowIdenticalIntegerInverted)
        For Each seg In task.edges.segments
            Dim nrc = New nrcData
            Dim segIndex = sortList.Count + 1
            nrc.rect = task.edges.rectList(segIndex - 1)
            nrc.mask = dst2(nrc.rect).InRange(segIndex, segIndex)
            nrc.pixels = seg.Count
            nrc.segment = seg
            sortList.Add(nrc.pixels, nrc)
        Next

        Dim nrcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim sortGridID As New SortedList(Of Integer, nrcData)(New compareAllowIdenticalInteger)
        Dim duplicatePixels As Integer
        For Each nrc In sortList.Values
            nrc.ID = task.grid.gridMap.Get(Of Integer)(nrc.segment(0).Y, nrc.segment(0).X)
            Dim takenFlag = nrcMap.Get(Of Byte)(nrc.segment(0).Y, nrc.segment(0).X)
            If takenFlag <> 0 Then
                duplicatePixels += nrc.pixels
                Continue For ' this id is already taken by a larger segment
            End If
            nrcMap(task.gridRects(nrc.ID)).SetTo(255)
            sortGridID.Add(nrc.ID, nrc)
        Next

        nrclist = New List(Of nrcData)(sortGridID.Values)

        dst1.SetTo(0)
        For i = 0 To nrclist.Count - 1
            Dim nrc = nrclist(i)
            dst1(nrc.rect).SetTo(nrc.ID Mod 255, nrc.mask)
        Next
        dst3 = ShowPaletteNoZero(dst1)

        labels(3) = CStr(nrclist.Count) + " segments are present.  " + CStr(duplicatePixels) +
                    " pixels were dropped because the segment hit an already occupied grid cell."
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

        Dim cppData(input.Total - 1) As Byte
        Marshal.Copy(input.Data, cppData, 0, cppData.Length)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim imagePtr = EdgeLineSimple_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), input.Rows, input.Cols, task.lineWidth * 2)
        handleSrc.Free()

        dst2 = cv.Mat.FromPixelData(input.Rows, input.Cols, cv.MatType.CV_8U, imagePtr).Clone
        Dim mm = GetMinMax(dst2)
        classCount = mm.maxVal

        Dim imageEdgeWidth = 2
        If dst2.Width >= 1280 Then imageEdgeWidth = 4
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






Public Class EdgeLine_BrickPoints : Inherits TaskParent
    Dim ptBrick As New BrickPoint_Basics
    Public classCount As Integer
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Find lines using the brick points"
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
        ptBrick.Run(src)
        labels(2) = ptBrick.labels(2)

        dst2 = task.edges.dst2
        dst3 = ShowPalette(task.edges.dst2)

        Dim segments(task.edges.classCount) As List(Of cv.Point2f)
        Dim brickCount As Integer, segmentCount As Integer
        For Each pt In ptBrick.ptList
            Dim val = task.edges.dst2.Get(Of Byte)(pt.Y, pt.X)
            If val > 0 And val < 255 Then
                If segments(val) Is Nothing Then
                    segments(val) = New List(Of cv.Point2f)
                    segmentCount += 1
                End If
                segments(val).Add(pt)
                brickCount += 1
            End If
        Next

        labels(3) = CStr(task.edges.classCount) + " segments were found and " + CStr(segmentCount) + " contained brick points"
        labels(3) += " " + CStr(brickCount) + " bricks were part of a segment"

        classCount = 0
        For Each segment In segments
            If segment Is Nothing Then Continue For
            classCount += 1
            Dim p1 = segment(0)
            For Each p2 In segment
                DrawCircle(dst3, p2)
                ' DrawLine(dst3, lp.p1, lp.p2)
                p1 = p2
            Next
        Next

        If standaloneTest() Then showSegment(dst1)
    End Sub
End Class





Public Class EdgeLine_DepthSegments : Inherits TaskParent
    Public segments As New List(Of List(Of cv.Point))
    Public Sub New()
        labels(3) = "Highlighting the individual segments one by one."
        desc = "Break up any edgeline segments that cross depth boundaries."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.edges.dst2

        segments.Clear()
        For Each seg In task.edges.segments
            Dim nextSeg As New List(Of cv.Point)
            Dim lastDepth = -1
            For Each pt In seg
                Dim depth = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
                If lastDepth > 0 And Math.Abs(lastDepth - depth) > 1 Then
                    If nextSeg.Count > 0 Then
                        segments.Add(nextSeg)
                        nextSeg.Clear()
                    End If
                End If

                If depth > 0 Then nextSeg.Add(pt)
                lastDepth = depth
            Next
            If nextSeg.Count > 0 Then segments.Add(nextSeg)
        Next

        dst3 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        Dim r = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        dst3.Rectangle(r, black, 4)
        If task.toggleOn Then
            SetTrueText("Segments without depth removed.", 3)
        Else
            dst3.SetTo(0, task.noDepthMask)
            SetTrueText("Segments with depth removed.", 3)
        End If
        labels(3) = "After using depth to isolate segments there are " + CStr(segments.Count) + " segments"
    End Sub
End Class