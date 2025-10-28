Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class RedColor_Basics : Inherits TaskParent
    Public classCount As Integer
    Public rcList As New List(Of rcData)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        task.redColor = Me
        cPtr = RedCloudMaxDist_Open()
        desc = "Run the C++ RedCloudMaxDist interface without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = srcMustBe8U(src)

        Dim imagePtr As IntPtr
        Dim inputData(dst1.Total - 1) As Byte
        Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        imagePtr = RedCloudMaxDist_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleInput.Free()
        dst0 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone

        classCount = RedCloudMaxDist_Count(cPtr)

        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4,
                                            RedCloudMaxDist_Rects(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)


        Dim rcListLast = New List(Of rcData)(rcList)
        Dim rcMapLast As cv.Mat = rcMap.Clone

        Dim minPixels As Integer = dst2.Total * 0.001
        Dim index As Integer = 1
        Dim newList As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To rects.Length - 4 Step 4
            Dim r = New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3))
            Dim rc = New rcData(dst0(r), r, index)
            If rc.pixels < minPixels Then Continue For
            newList.Add(rc.pixels, rc)
            index += 1
        Next

        Dim r2 As cv.Rect
        Dim count As Integer
        rcList.Clear()
        Dim usedColor As New List(Of cv.Scalar)
        For Each rc In newList.Values
            Dim r1 = rc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast As Integer = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If indexLast > 0 And indexLast < rcListLast.Count Then
                indexLast -= 1 ' index is 1 less than the rcMap value
                r2 = rcListLast(indexLast).rect
            Else
                indexLast = -1
            End If
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                rc.age = rcListLast(indexLast).age + 1
                rc.color = rcListLast(indexLast).color
                If rc.age >= 1000 Then rc.age = 2
                count += 1
            End If

            If usedColor.Contains(rc.color) Then
                rc.color = randomCellColor()
                rc.age = 1
            End If
            usedColor.Add(rc.color)

            rc.index = rcList.Count + 1
            rcList.Add(rc)
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
            SetTrueText(CStr(rc.age), rc.maxDist)
        Next

        dst2.SetTo(0)
        For Each rc In rcList
            rc.mask = rcMap(rc.rect).InRange(rc.index, rc.index)
            rc.buildMaxDist()
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
        Next

        If standaloneTest() Then
            RedCell_Basics.selectCell(rcMap, rcList)
            If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
            SetTrueText(strOut, 3)
        End If

        labels(2) = CStr(classCount) + " cells found. " + CStr(rcList.Count) + " >" +
                    " minpixels (" + Format(rcList.Count / classCount, "0%") + ").  " + CStr(count) +
                    " matched to previous generation"
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloudMaxDist_Close(cPtr)
    End Sub
End Class





Public Class RedColor_BasicsSlow : Inherits TaskParent
    Public redSweep As New RedColor_Sweep
    Public rcList As New List(Of rcData)
    Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Track the RedColor cells from RedColor_Core"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        redSweep.Run(src)
        dst3 = redSweep.dst3

        Static rcListLast = New List(Of rcData)(redSweep.rcList)
        Static rcMapLast As cv.Mat = redSweep.rcMap.clone

        rcList.Clear()
        Dim r2 As cv.Rect
        rcMap.SetTo(0)
        dst2.SetTo(0)
        For Each rc In redSweep.rcList
            Dim r1 = rc.rect
            r2 = New cv.Rect(0, 0, 1, 1) ' fake rect for conditional below...
            Dim indexLast = rcMapLast.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X) - 1
            If indexLast > 0 Then r2 = rcListLast(indexLast).rect
            If indexLast >= 0 And r1.IntersectsWith(r2) And task.optionsChanged = False Then
                rc.age = rcListLast(indexLast).age + 1
                If rc.age >= 1000 Then rc.age = 2
                If task.heartBeat = False And rc.rect.Contains(rcListLast(indexLast).maxdist) Then
                    rc.maxDist = rcListLast(indexLast).maxdist
                End If
                rc.color = rcListLast(indexLast).color
            End If
            rc.index = rcList.Count + 1
            rcMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)
            If standaloneTest() Then
                dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
                SetTrueText(CStr(rc.age), rc.maxDist)
            End If
            rcList.Add(rc)
        Next

        labels(2) = CStr(rcList.Count) + " regions were identified "
        labels(3) = redSweep.labels(3)

        rcListLast = New List(Of rcData)(rcList)
        rcMapLast = rcMap.Clone

        RedCell_Basics.selectCell(rcMap, rcList)
        If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
        SetTrueText(strOut, 1)
    End Sub
End Class






Public Class RedColor_BasicsFast : Inherits TaskParent
    Public classCount As Integer
    Public RectList As New List(Of cv.Rect)
    Public Sub New()
        cPtr = RedCloudMaxDist_Open()
        desc = "Run the C++ RedCloudMaxDist interface without a mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst1 = srcMustBe8U(src)

        Dim imagePtr As IntPtr
        Dim inputData(dst1.Total - 1) As Byte
        Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        imagePtr = RedCloudMaxDist_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
        handleInput.Free()
        dst3 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone
        dst2 = PaletteFull(dst3)

        classCount = RedCloudMaxDist_Count(cPtr)
        labels(3) = "CV_8U version with " + CStr(classCount) + " cells."

        If classCount = 0 Then Exit Sub ' no data to process.

        Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloudMaxDist_Rects(cPtr))

        Dim rects(classCount * 4) As Integer
        Marshal.Copy(rectData.Data, rects, 0, rects.Length)

        Dim minPixels = dst2.Total * 0.001
        RectList.Clear()

        For i = 0 To rects.Length - 4 Step 4
            Dim r = New cv.Rect(rects(i), rects(i + 1), rects(i + 2), rects(i + 3))
            If r.Width * r.Height >= minPixels Then RectList.Add(r)
        Next
        labels(2) = CStr(RectList.Count) + " cells were found."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloudMaxDist_Close(cPtr)
    End Sub
End Class





Public Class RedColor_HeartBeat : Inherits TaskParent
    Dim redCore As New RedColor_BasicsSlow
    Public rcList As New List(Of rcData)
    Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        desc = "Run RedColor_Core on the heartbeat but just floodFill at maxDist otherwise."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static rcLost As New List(Of Integer)
        If task.heartBeat Or task.optionsChanged Then
            rcLost.Clear()
            redCore.Run(src)
            dst2 = redCore.dst2
            labels(2) = redCore.labels(2)
        Else
            If src.Type <> cv.MatType.CV_8U Then src = task.gray
            redCore.redSweep.reduction.Run(src)
            dst1 = redCore.redSweep.reduction.dst2 + 1

            Dim index As Integer = 1
            Dim rect As New cv.Rect
            Dim maskRect = New cv.Rect(1, 1, dst1.Width, dst1.Height)
            Dim mask = New cv.Mat(New cv.Size(dst1.Width + 2, dst1.Height + 2), cv.MatType.CV_8U, 0)
            Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
            Dim minCount = dst1.Total * 0.001
            rcList.Clear()
            rcMap.SetTo(0)
            For Each rc In redCore.rcList
                Dim pt = rc.maxDist
                If rcMap.Get(Of Byte)(pt.Y, pt.X) = 0 Then
                    Dim count = cv.Cv2.FloodFill(dst1, mask, pt, index, rect, 0, 0, flags)
                    If count > minCount Then
                        Dim pcc = New rcData(dst1(rect), rect, index)
                        If pcc.index >= 0 Then
                            pcc.color = rc.color
                            pcc.age = rc.age + 1
                            rcList.Add(pcc)
                            rcMap(pcc.rect).SetTo(pcc.index Mod 255, pcc.mask)
                            index += 1
                        End If
                    Else
                        If rcLost.Contains(rc.index - 1) = False Then rcLost.Add(rc.index - 1)
                    End If
                End If
            Next

            dst2 = PaletteBlackZero(rcMap)
            labels(2) = CStr(rcList.Count) + " regions were identified "
        End If

        If standaloneTest() Then
            For Each rc In rcList
                dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
            Next

            dst3.SetTo(0)
            For Each index In rcLost
                Dim rc = redCore.rcList(index)
                dst3(rc.rect).SetTo(rc.color, rc.mask)
            Next
            labels(3) = "There were " + CStr(rcLost.Count) + " cells temporarily lost."

            RedCell_Basics.selectCell(rcMap, rcList)
            If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
            SetTrueText(strOut, 3)
        End If
    End Sub
End Class




Public Class RedColor_Sweep : Inherits TaskParent
    Public rcList As New List(Of rcData)
    Public reduction As New Reduction_Basics
    Public rcMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        desc = "Find RedColor cells in the reduced color image using a simple floodfill loop."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_8U Then src = task.gray
        reduction.Run(src)
        dst3 = reduction.dst2 + 1
        labels(3) = reduction.labels(2)

        Dim index As Integer = 1
        Dim rect As New cv.Rect
        Dim maskRect = New cv.Rect(1, 1, dst3.Width, dst3.Height)
        Dim mask = New cv.Mat(New cv.Size(dst3.Width + 2, dst3.Height + 2), cv.MatType.CV_8U, 0)
        Dim flags As cv.FloodFillFlags = cv.FloodFillFlags.Link4 ' Or cv.FloodFillFlags.MaskOnly ' maskonly is expensive but why?
        Dim minCount = dst3.Total * 0.001
        rcList.Clear()
        rcMap.SetTo(0)
        For y = 0 To dst3.Height - 1
            For x = 0 To dst3.Width - 1
                Dim pt = New cv.Point(x, y)
                If dst3.Get(Of Byte)(pt.Y, pt.X) > 0 Then
                    Dim count = cv.Cv2.FloodFill(dst3, mask, pt, index, rect, 0, 0, flags)
                    If count > minCount Then
                        Dim rc = New rcData(dst3(rect), rect, index)
                        If rc.index >= 0 Then
                            rcList.Add(rc)
                            rcMap(rc.rect).SetTo(rc.index Mod 255, rc.mask)
                            index += 1
                        End If
                    Else
                        If rect.Width > 0 And rect.Height > 0 Then dst3(rect).SetTo(255, mask(rect))
                    End If
                End If
            Next
        Next

        dst2 = PaletteBlackZero(rcMap)

        If standaloneTest() Then
            For Each rc In rcList
                dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)
            Next

            RedCell_Basics.selectCell(rcMap, rcList)
            If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
            SetTrueText(strOut, 3)
        End If

        labels(2) = CStr(rcList.Count) + " regions were identified "
    End Sub
End Class





Public Class RedColor_LeftRight : Inherits TaskParent
    Dim redLeft As New RedColor_Basics
    Dim redRight As New RedColor_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        desc = "Display the RedColor_Basics output for both the left and right images."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(task.leftView)

        redLeft.Run(reduction.dst2)
        dst2 = PaletteFull(redLeft.dst2)
        labels(2) = redLeft.labels(2) + " in the left image"

        reduction.Run(task.rightView)

        redRight.Run(reduction.dst2)
        dst3 = PaletteFull(redRight.dst2)
        labels(3) = redRight.labels(2) + " in the right image"
    End Sub
End Class