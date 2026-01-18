Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class RedColor_Basics : Inherits TaskParent
        Implements IDisposable
        Public classCount As Integer
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            task.redColor = Me
            cPtr = RedCloud_Open()
            desc = "Run the C++ RedCloud interface without a mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim imagePtr As IntPtr
            Dim inputData(dst1.Total - 1) As Byte
            Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleInput.Free()
            dst0 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone

            classCount = RedCloud_Count(cPtr)

            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))

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

            rcList.Clear()
            dst2.SetTo(0)
            Dim changed As Integer
            Dim usedColor As New List(Of cv.Scalar)
            For Each rc In newList.Values
                Dim maxDist = rc.maxDist
                rc = RedCloud_Basics.rcDataMatch(rc, rcListLast, rcMapLast)

                rc.index = rcList.Count + 1

                ' The first cell often contains other cells completely within it.
                ' These often cause the maxdist to move around.
                ' So just fix the color here and create a stable image.
                ' The cells within the largest cell will switch colors but many cells are stable.
                If rc.index = 1 Then rc.color = blue
                If maxDist <> rc.maxDist Then changed += 1

                rcMap(rc.rect).SetTo(rc.index, rc.mask)

                If usedColor.Contains(rc.color) Then
                    rc.color = Palette_Basics.randomCellColor()
                    rc.age = 1
                End If
                usedColor.Add(rc.color)

                rcList.Add(rc)

                dst2(rc.rect).SetTo(rc.color, rc.mask)
                dst2.Circle(rc.maxDist, task.DotSize, task.highlight, -1)

                SetTrueText(CStr(rc.age), rc.maxDist)
            Next
            If standaloneTest() Then
                RedCloud_Cell.selectCell(rcMap, rcList)
                If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
                SetTrueText(strOut, 3)
            End If

            labels(2) = CStr(classCount) + " RedColor cells. " + CStr(rcList.Count) + " cells >" +
                        " minpixels.  " + CStr(rcList.Count - changed) + " matched to previous generation"
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
        End Sub
    End Class




    Public Class NR_RedColor_CPP : Inherits TaskParent
        Implements IDisposable
        Public classCount As Integer
        Public rcList As New List(Of rcData)
        Public rcMap As cv.Mat = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public Sub New()
            cPtr = RedCloud_Open()
            desc = "Run the C++ RedCloud interface without a mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim imagePtr As IntPtr
            Dim inputData(dst1.Total - 1) As Byte
            Marshal.Copy(dst1.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols)
            handleInput.Free()
            dst0 = cv.Mat.FromPixelData(dst1.Rows, dst1.Cols, cv.MatType.CV_8U, imagePtr).Clone

            classCount = RedCloud_Count(cPtr)

            If classCount = 0 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4,
                                            RedCloud_Rects(cPtr))

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
                    rc.color = Palette_Basics.randomCellColor()
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
                RedCloud_Cell.selectCell(rcMap, rcList)
                If task.rcD IsNot Nothing Then strOut = task.rcD.displayCell()
                SetTrueText(strOut, 3)
            End If

            labels(2) = CStr(classCount) + " cells. " + CStr(rcList.Count) + " cells >" +
                    " minpixels.  " + CStr(count) + " matched to previous generation"
        End Sub
        Public Overloads Sub Dispose() Implements IDisposable.Dispose
            If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
        End Sub
    End Class






    Public Class NR_RedColor_LeftRight : Inherits TaskParent
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





    Public Class NR_RedColor_NWay : Inherits TaskParent
        Dim binN As New BinNWay_Basics
        Public Sub New()
            desc = "Run RedColor on the output of the BinNWay_Basics"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            binN.Run(src)
            dst3 = binN.dst3
            labels(3) = binN.labels(3)

            dst2 = runRedColor(binN.dst2, labels(2))
        End Sub
    End Class
End Namespace