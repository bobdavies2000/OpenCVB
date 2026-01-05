Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class EdgeLine_Basics : Inherits TaskParent
        Public rcList As New List(Of rcData)
        Public rcMap As New cv.Mat
        Public classCount As Integer
        Public Sub New()
            cPtr = EdgeLineRaw_Open()
            labels(3) = "Palette version of dst2"
            If standalone Then task.gOptions.showMotionMask.Checked = True
            desc = "Use EdgeLines to find edges/lines but without using motionMask directly"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.grayStable

            Dim cppData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, cppData, 0, cppData.Length)
            Dim handlesrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
            Dim imagePtr = EdgeLineRaw_RunCPP(cPtr, handlesrc.AddrOfPinnedObject(), src.Rows, src.Cols,
                                              task.lineWidth)
            handlesrc.Free()
            rcMap = cv.Mat.FromPixelData(src.Rows, src.Cols, cv.MatType.CV_32S, imagePtr)
            rcMap.ConvertTo(dst2, cv.MatType.CV_8U)

            Dim imageEdgeWidth = If(dst2.Width >= 1280, 4, 2)
            ' prevent leaks at the image boundary...
            dst2.Rectangle(New cv.Rect(0, 0, dst2.Width - 1, dst2.Height - 1), 255, imageEdgeWidth)

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, EdgeLineRaw_Rects(cPtr))

            classCount = Math.Min(EdgeLineRaw_GetSegCount(cPtr), 255)
            If classCount = 0 Then Exit Sub ' nothing to work with....
            Dim rects(classCount * 4) As Integer
            Marshal.Copy(rectData.Data, rects, 0, rects.Length)

            dst3.SetTo(0)
            rcList.Clear()
            For i = 0 To classCount * 4 - 4 Step 4
                Dim r = New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3))
                Dim index = rcList.Count + 1
                Dim mask = rcMap(r)
                Dim rc = New rcData(mask, r, index, 0)

                rcList.Add(rc)
                If standaloneTest() Then dst3(rc.rect).SetTo(task.scalarColors(rc.gridIndex), rc.mask)
            Next

            labels(2) = CStr(classCount) + " line segments were found with motion threshold of " +
                        CStr(task.motionThreshold) + " pixels changed in a grid rect."
        End Sub
        Public Sub Close()
            EdgeLineRaw_Close(cPtr)
        End Sub
    End Class





    Public Class EdgeLine_Motion : Inherits TaskParent
        Dim edgeLine As New EdgeLine_Basics
        Public rcList As New List(Of rcData)
        Public classCount As Integer
        Public Sub New()
            If standalone Then task.gOptions.showMotionMask.Checked = True
            labels(1) = "CV_8U edges - input to PalleteBlackZero"
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32F, 0)
            desc = "Retain edges where there was no motion."
        End Sub
        Private Sub rcDataDraw(rc As rcData)
            Static nextList = New List(Of List(Of cv.Point))
            Dim n = rc.contour.Count - 1
            nextList.Clear()
            nextList.Add(rc.contour)
            cv.Cv2.Polylines(dst2(rc.rect), nextList, False, cv.Scalar.All(rc.index), task.lineWidth, task.lineType)
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim histogram As New cv.Mat
            Dim histarray(edgeLine.rcList.Count - 1) As Single
            If task.motionBasics.motionList.Count = 0 Then Exit Sub ' no change!

            Dim newList As New List(Of rcData)
            dst2.SetTo(0)
            If edgeLine.rcList.Count Then
                Dim ranges1 = New cv.Rangef() {New cv.Rangef(0, edgeLine.rcList.Count)}
                cv.Cv2.CalcHist({dst2}, {0}, task.motionMask, histogram,
                            1, {edgeLine.rcList.Count}, ranges1)
                Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

                For i = 1 To histarray.Count - 1
                    If histarray(i) = 0 Then
                        Dim rc = edgeLine.rcList(i - 1)
                        rc.index = newList.Count + 1
                        newList.Add(rc)

                        rcDataDraw(rc)
                    End If
                Next
            End If
            Dim removed = edgeLine.rcList.Count - newList.Count

            edgeLine.Run(src)
            If edgeLine.classCount = 0 Then Exit Sub
            ReDim histarray(edgeLine.classCount - 1)

            Dim ranges2 = New cv.Rangef() {New cv.Rangef(0, edgeLine.classCount)}
            cv.Cv2.CalcHist({edgeLine.dst2}, {0}, task.motionMask, histogram,
                        1, {edgeLine.classCount}, ranges2)
            Marshal.Copy(histogram.Data, histarray, 0, histarray.Length)

            Dim count As Integer
            For Each rc In edgeLine.rcList
                If histarray(rc.index - 1) > 0 And rc.contour.Count > 0 Then
                    count += 1
                    rc.index = newList.Count + 1
                    If rc.contour.Count > 0 Then
                        Dim gIndex = task.gridMap.Get(Of Integer)(rc.contour(0).Y, rc.contour(0).X)
                        rc.color = task.vecColors(gIndex Mod 255)
                    End If
                    newList.Add(rc)

                    rcDataDraw(rc)
                End If
            Next

            dst2.ConvertTo(dst1, cv.MatType.CV_8U)
            dst3 = PaletteBlackZero(dst1)

            rcList = New List(Of rcData)(newList)
            classCount = rcList.Count

            labels(2) = CStr(edgeLine.classCount) + " lines found. " +
                    CStr(removed) + " removed and " + CStr(count) + " added " +
                    " to rcList after filtering for motion."
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






    Public Class EdgeLine_BrickPoints : Inherits TaskParent
        Dim bPoint As New BrickPoint_Basics
        Public classCount As Integer
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels(1) = "EdgeLine segments displayed one for each frame starting with the longest."
            desc = "Find lines using the brick points"
        End Sub
        Public Sub showSegment(dst As cv.Mat)
            If task.quarterBeat Then
                Static debugSegment = 0
                debugSegment += 1
                edgeline.Run(task.grayStable)
                If debugSegment >= edgeline.classCount Then
                    debugSegment = 0
                    dst.SetTo(0)
                End If
                If debugSegment >= edgeline.classCount Then debugSegment = 0
                If debugSegment Then
                    edgeline.dst1 = edgeline.dst2.InRange(debugSegment, debugSegment)
                    edgeline.dst1.CopyTo(dst, edgeline.dst1)
                End If
                debugSegment += 1
            End If
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edgeline.Run(task.grayStable)
            bPoint.Run(src)
            labels(2) = bPoint.labels(2)

            dst2 = edgeline.dst2
            dst3 = PaletteBlackZero(edgeline.dst2)

            Dim segments(edgeline.classCount) As List(Of cv.Point2f)
            Dim brickCount As Integer, segmentCount As Integer
            For Each pt In bPoint.ptList
                Dim val = edgeline.dst2.Get(Of Byte)(pt.Y, pt.X)
                If val > 0 And val < 255 Then
                    If segments(val) Is Nothing Then
                        segments(val) = New List(Of cv.Point2f)
                        segmentCount += 1
                    End If
                    segments(val).Add(pt)
                    brickCount += 1
                End If
            Next

            labels(3) = CStr(edgeline.classCount) + " segments were found and " + CStr(segmentCount) + " contained brick points"
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
        Dim edgeline As New EdgeLine_Basics
        Public Sub New()
            labels(3) = "Highlighting the individual line segments one by one."
            desc = "Break up any edgeline segments that cross depth boundaries."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edgeline.Run(task.grayStable)
            dst2 = edgeline.dst2

            segments.Clear()
            For Each rc In edgeline.rcList
                Dim nextSeg As New List(Of cv.Point)
                Dim lastDepth = -1
                For Each pt In rc.contour
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








    Public Class EdgeLine_LeftRight : Inherits TaskParent
        Dim edges As New EdgeLine_Basics
        Public Sub New()
            labels(3) = "Right View: Note it is updated on every frame - it cannot use the motion mask."
            desc = "Build the left and right edge lines."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            edges.Run(task.leftViewStable)
            dst2 = edges.dst2.Clone

            edges.Run(task.rightView)
            dst3 = edges.dst2.Clone
        End Sub
    End Class

End Namespace