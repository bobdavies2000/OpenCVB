Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Namespace VBClasses
    Public Class RedMask_Basics : Inherits TaskParent
        Implements IDisposable
        Public mdList As New List(Of maskData)
        Public classCount As Integer
        Public Sub New()
            cPtr = RedMask_Open()
            desc = "Run the C++ RedMask to create a list of mask, rect, and other info about image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst1 = Mat_Basics.srcMustBe8U(src)

            Dim inputData(dst1.Total - 1) As Byte
            dst1.GetArray(Of Byte)(inputData)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim minSize As Integer = dst2.Total * 0.001
            Dim imagePtr = RedMask_Run(cPtr, handleInput.AddrOfPinnedObject(), dst1.Rows, dst1.Cols, minSize)
            handleInput.Free()
            dst2 = cv.Mat.FromPixelData(dst1.Rows + 2, dst1.Cols + 2, cv.MatType.CV_8U, imagePtr).Clone
            dst2 = dst2(New cv.Rect(1, 1, dst2.Width - 2, dst2.Height - 2))

            classCount = RedMask_Count(cPtr)
            If classCount <= 1 Then Exit Sub ' no data to process.

            Dim rectData = cv.Mat.FromPixelData(classCount, 1, cv.MatType.CV_32SC4, RedMask_Rects(cPtr))
            Dim rects(classCount - 1) As cv.Rect
            rectData.GetArray(Of cv.Rect)(rects)

            Dim rectlist = rects.ToList
            mdList.Clear()
            mdList.Add(New maskData) ' add a placeholder for zero...
            For i = 0 To classCount - 1
                Dim md As New maskData
                md.rect = rectlist(i)
                If md.rect.Size = dst2.Size Then Continue For
                md.mask = dst2(md.rect).InRange(i + 1, i + 1)
                md.contour = ContourBuild(md.mask)
                DrawTour(md.mask, md.contour, 255, -1)
                md.pixels = md.mask.CountNonZero
                md.maxDist = Distance_Basics.GetMaxDist(md)
                md.mm = GetMinMax(task.pcSplit(2)(md.rect), task.depthmask(md.rect))
                md.index = mdList.Count
                mdList.Add(md)
            Next

            classCount = mdList.Count

            dst3 = Palettize(dst2)

            labels(2) = "CV_8U result with " + CStr(classCount) + " regions."
            labels(3) = "Palette version of the data in dst2 with " + CStr(classCount) + " regions."
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = RedMask_Close(cPtr)
        End Sub
    End Class




    Public Class NR_RedMask_Redraw : Inherits TaskParent
        Public redMask As New RedMask_Basics
        Public Sub New()
            desc = "Redraw the image using the mean color of each cell."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redMask.Run(src)

            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            Dim color As cv.Scalar
            Dim emptyVec As New cv.Vec3b
            For Each md In redMask.mdList
                Dim c = src.Get(Of cv.Vec3b)(md.maxDist.Y, md.maxDist.X)
                color = New cv.Scalar(c.Item0, c.Item1, c.Item2)
                dst2(md.rect).SetTo(color, md.mask)
            Next
            labels(2) = redMask.labels(2)
        End Sub
    End Class






    Public Class RedMask_Color : Inherits TaskParent
        Dim cellGen As New RedMask_ToRedColor
        Dim redMask As New RedMask_Basics
        Public rclist As New List(Of rcData)
        Public rcMap As New cv.Mat ' redColor map 
        Dim contours As New Contour_Basics
        Public inputRemoved As cv.Mat
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Find cells and then match them to the previous generation with minimum boundary"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            If src.Type <> cv.MatType.CV_8U Then
                If standalone And task.fOptions.Color8USource.SelectedItem = "EdgeLine_Basics" Then
                    dst1 = contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                Else
                    dst1 = Mat_Basics.srcMustBe8U(src)
                End If
            Else
                dst1 = src
            End If

            If inputRemoved IsNot Nothing Then dst1.SetTo(0, inputRemoved)
            redMask.Run(dst1 + 1)

            If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
            cellGen.mdList = redMask.mdList
            cellGen.Run(redMask.dst2)

            rclist.Clear()
            For Each md In redMask.mdList
                Dim rc = New rcData(md.mask, md.rect, rclist.Count + 1)
                rc.buildMaxDist()
                rclist.Add(rc)
            Next

            dst2 = redMask.dst2
            dst3 = Palettize(dst2)
            labels(2) = redMask.labels(2)
            labels(3) = CStr(rclist.Count) + " loosely defined cells found"

            dst2.ConvertTo(rcMap, cv.MatType.CV_32S)
            strOut = RedUtil_Basics.selectCell(rcMap, rclist)
            SetTrueText(strOut, 1)
        End Sub
    End Class




    Public Class RedMask_ToRedColor : Inherits TaskParent
        Public redMask As New RedMask_Basics
        Public mdList As New List(Of maskData)
        Public rcList As New List(Of rcData)
        Dim rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
        Public Sub New()
            desc = "Generate the RedColor cells from the rects, mask, and pixel counts."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            redMask.Run(src)
            dst2 = redMask.dst3
            labels(2) = redMask.labels(3)

            Dim rcMapLast = rcMap.Clone
            Dim rcListLast As New List(Of rcData)(rcList)
            Dim sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
            rcMap.SetTo(0)
            For i = 0 To redMask.mdList.Count - 1
                Dim rc = New rcData(redMask.mdList(i).mask, redMask.mdList(i).rect, sortedCells.Count + 1)
                If rc.rect.Size = dst2.Size Then Continue For ' RedMask_List can find a cell this big.  
                DrawTour(rc.mask, rc.contour, 255, -1)
                rc.pixels = redMask.mdList(i).mask.CountNonZero
                rc.age = 1
                sortedCells.Add(rc.pixels, rc)

                rcMap(rc.rect).SetTo(rc.index, rc.mask)
            Next

            rcList = New List(Of rcData)(sortedCells.Values)
            labels(2) = CStr(rcList.Count) + " total cells "

            If standalone Then
                strOut = RedUtil_Basics.selectCell(rcMap, rcList)
                SetTrueText(strOut, 3)
            End If
        End Sub
    End Class






    Public Class RedMask_Flippers : Inherits TaskParent
        Public flipCells As New List(Of rcData)
        Public nonFlipCells As New List(Of rcData)
        Dim redMask As New RedMask_Basics
        Public Sub New()
            labels(3) = "Highlighted below are the cells which flipped in color from the previous frame."
            desc = "Identify the cells that are changing color because they were split or lost."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst3 = runRedList(src, labels(3))
            redMask.Run(src)
            dst3 = redMask.dst2
            labels(3) = redMask.labels(2)

            Static lastMap As cv.Mat = RedMask_List.DisplayCells(redMask.mdList)

            Dim unMatched As Integer
            Dim unMatchedPixels As Integer
            flipCells.Clear()
            nonFlipCells.Clear()
            dst2.SetTo(0)
            Dim currMap = XO_RedList_Basics.DisplayCells()
            For Each md In redMask.mdList
                Dim rc = New rcData(md.mask, md.rect, md.index)

                Dim lastColor = lastMap.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                Dim currColor = currMap.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                If lastColor <> currColor Then
                    unMatched += 1
                    unMatchedPixels += rc.pixels
                    flipCells.Add(rc)
                    dst2(rc.rect).SetTo(rc.color, rc.mask)
                Else
                    nonFlipCells.Add(rc)
                End If
            Next

            lastMap = currMap.Clone

            If task.heartBeat Then
                labels(2) = CStr(unMatched) + " of " + CStr(redMask.mdList.Count) + " cells changed " +
                        " tracking color, totaling " + CStr(unMatchedPixels) + " pixels."
            End If
        End Sub
    End Class





    Public Class RedMask_List : Inherits TaskParent
        Public inputRemoved As cv.Mat
        Public cellGen As New RedMask_ToRedColor
        Public redMask As New RedMask_Basics
        Public contours As New Contour_Basics
        Public rcMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Public rclist As New List(Of rcData)
        Public Sub New()
            task.gOptions.displayDst1.Checked = True
            desc = "Find cells and then match them to the previous generation with minimum boundary"
        End Sub
        Public Shared Function DisplayCells(mdList As List(Of maskData)) As cv.Mat
            Dim dst As New cv.Mat(task.workRes, cv.MatType.CV_8UC3, 0)

            For Each md In mdList
                dst(md.rect).SetTo(task.scalarColors(md.index Mod 255), md.mask)
            Next

            Return dst
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            contours.Run(src)
            If src.Type <> cv.MatType.CV_8U Then
                If standalone And task.fOptions.Color8USource.SelectedItem = "EdgeLine_Basics" Then
                    dst1 = contours.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
                Else
                    dst1 = Mat_Basics.srcMustBe8U(src)
                End If
            Else
                dst1 = src
            End If

            If inputRemoved IsNot Nothing Then dst1.SetTo(0, inputRemoved)
            redMask.Run(dst1)
            dst2 = redMask.dst3
            labels(2) = redMask.labels(3)

            If redMask.mdList.Count = 0 Then Exit Sub ' no data to process.
            rcMap.SetTo(0)
            rclist.Clear()
            For Each md In redMask.mdList
                Dim rc = New rcData(md.mask, md.rect, md.index)
                rc.index = rclist.Count + 1
                rclist.Add(rc)
                rcMap(rc.rect).SetTo(rc.index, rc.mask)
            Next

            strOut = RedUtil_Basics.selectCell(rcMap, rclist)
            SetTrueText(strOut, 1)
        End Sub
    End Class
End Namespace