Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedColor_Basics : Inherits VB_Algorithm
    Public minCore As New RedColor_Core
    Public redCells As New List(Of rcData)
    Dim lastColors As cv.Mat
    Dim lastMap As cv.Mat = dst2.Clone
    Public showMaxIndex = 20
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        lastColors = dst3.Clone
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        minCore.Run(src)
        Dim lastCells As New List(Of rcData)(redCells)

        redCells.Clear()
        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Vec3b)({black})
        Dim unmatched As Integer
        For Each key In minCore.sortedCells
            Dim cell = key.Value
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)
            If index < lastCells.Count Then
                cell.color = lastColors.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
                'cell.maxDist = lastCells(index).maxDist
            Else
                unmatched += 1
            End If
            If usedColors.Contains(cell.color) Then
                unmatched += 1
                cell.color = randomCellColor()
            End If
            usedColors.Add(cell.color)

            If dst2.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = redCells.Count
                redCells.Add(cell)
                dst2(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)
            End If
        Next

        If standalone Or showIntermediate() Then identifyCells(redCells, showMaxIndex)

        labels(3) = CStr(redCells.Count) + " cells were identified.  The top " + CStr(showMaxIndex) + " are numbered"
        labels(2) = minCore.labels(3) + " " + CStr(unmatched) + " cells were not matched to previous frame."
        task.cellSelect = New rcData
        If task.clickPoint = New cv.Point(0, 0) Then
            If redCells.Count > 2 Then
                task.clickPoint = redCells(0).maxDist
                task.cellSelect = redCells(0)
            End If
        Else
            Dim index = dst2.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            If index <> 0 Then task.cellSelect = redCells(index - 1)
        End If
        lastColors = dst3.Clone
        lastMap = dst2.Clone
        If redCells.Count > 0 Then dst1 = vbPalette(lastMap * 255 / redCells.Count)
    End Sub
End Class









'Public Class RedCloud_CPP : Inherits VB_Algorithm
'    Public redCells As New List(Of rcData)
'    Public cellCount As Integer
'    Public inputMask As cv.Mat
'    Public Sub New()
'        cPtr = RedCloud_Open()
'        desc = "Floodfill every pixel in the prepared input."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        If task.optionsChanged Then inputMask = Nothing
'        If src.Channels <> 1 Then
'            Static colorClass As New Color_Basics
'            colorClass.Run(src)
'            src = colorClass.dst2
'        End If

'        Dim inputData(src.Total - 1) As Byte
'        Marshal.Copy(src.Data, inputData, 0, inputData.Length)

'        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

'        If redOptions.UseDepth.Checked Or inputMask IsNot Nothing Then
'            If inputMask Is Nothing Then inputMask = task.noDepthMask
'            Dim maskData(inputMask.Total - 1) As Byte
'            Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length)
'            Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)
'            cellCount = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(),
'                                      src.Rows, src.Cols, 250)
'            handleMask.Free()
'        Else
'            cellCount = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols, 250)
'        End If
'        handleInput.Free()

'        If cellCount = 0 Then Exit Sub ' no depth yet...

'        Dim ptData = New cv.Mat(cellCount, 1, cv.MatType.CV_32SC2, RedCloud_FloodPointList(cPtr))

'        Dim floodPoints As New List(Of cv.Point)
'        For i = 0 To cellCount - 1
'            floodPoints.Add(ptData.Get(Of cv.Point)(i, 0))
'        Next

'        Dim floodFlag = 4 Or cv.FloodFillFlags.FixedRange

'        redCells.Clear()
'        Dim other = New rcData
'        other.mask = New cv.Mat(1, 1, cv.MatType.CV_8U, 255)
'        other.rect = New cv.Rect(0, 0, 1, 1)
'        redCells.Add(other)
'        Dim mask As New cv.Mat(src.Height + 2, src.Width + 2, cv.MatType.CV_8U, 0)
'        If redOptions.UseDepth.Checked Or inputMask IsNot Nothing Then
'            inputMask.CopyTo(mask(New cv.Rect(1, 1, mask.Width - 2, mask.Height - 2)))
'            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 255, 1)
'        End If
'        Dim fill As Integer
'        Dim totalPixels As Integer
'        cellCount = 1
'        Dim colorRun = redOptions.UseColor.Checked
'        For i = 0 To floodPoints.Count - 1
'            Dim rc As New rcData
'            fill = cellCount
'            rc.floodPoint = floodPoints(i)
'            If mask.Get(Of Byte)(rc.floodPoint.Y, rc.floodPoint.X) = 0 Then
'                If colorRun Then
'                    If task.depthOutline.Get(Of Byte)(rc.floodPoint.Y, rc.floodPoint.X) <> 0 Then Continue For
'                End If
'                rc.index = cellCount
'                rc.pixels = src.FloodFill(mask, rc.floodPoint, New cv.Scalar(fill), rc.rect, 0, 0, floodFlag Or fill << 8)
'                If rc.rect.Width = 0 Then Continue For
'                rc.mask = mask(rc.rect).InRange(fill, fill)
'                redCells.Add(rc)
'                totalPixels += rc.pixels
'                cellCount += 1
'            End If
'        Next

'        dst2 = src
'        dst3 = vbPalette(dst2 * 255 / cellCount)
'        If heartBeat() Then
'            labels(2) = "Found " + CStr(cellCount) + " cells - " + Format(totalPixels / src.Total, "0%") +
'                        " of the image."
'        End If
'    End Sub
'    Public Sub Close()
'        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
'    End Sub
'End Class






Public Class RedColor_Core : Inherits VB_Algorithm
    Public sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
    Public inputMask As cv.Mat
    Public Sub New()
        cPtr = FloodCell_Open()
        desc = "Core interface to the C++ code for floodfill."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            Static colorClass As New Color_Basics
            colorClass.Run(src)
            src = colorClass.dst2
        End If

        Dim imagePtr As IntPtr
        If inputMask Is Nothing Then
            Dim inputData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = FloodCell_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols,
                                     src.Type, redOptions.DesiredCellSlider.Value, 0)
            handleInput.Free()
        Else
            Dim inputData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim maskData(inputMask.Total - 1) As Byte
            Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length)
            Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

            imagePtr = FloodCell_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols,
                                     src.Type, redOptions.DesiredCellSlider.Value, 0)
            handleMask.Free()
            handleInput.Free()
        End If

        Dim classCount = FloodCell_Count(cPtr)
        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone
        dst3 = vbPalette(dst2 * 255 / classCount)

        If heartBeat() Then labels(3) = CStr(classCount) + " cells found"
        If classCount <= 1 Then Exit Sub

        Dim sizeData = New cv.Mat(classCount, 1, cv.MatType.CV_32S, FloodCell_Sizes(cPtr))
        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, FloodCell_Rects(cPtr))
        Dim floodPointData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC2, FloodCell_FloodPoints(cPtr))
        sortedCells.Clear()
        For i = 0 To classCount - 1
            Dim rc As New rcData
            rc.index = i + 1
            rc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            rc.mask = dst2(rc.rect).InRange(rc.index, rc.index).Threshold(0, 255, cv.ThresholdTypes.Binary)
            'Dim contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            'vbDrawContour(rc.mask, contour, 255, -1)

            rc.pixels = sizeData.Get(Of Integer)(i, 0)
            rc.floodPoint = floodPointData.Get(Of cv.Point)(i, 0)
            rc.mask.Rectangle(New cv.Rect(0, 0, rc.mask.Width, rc.mask.Height), 0, 1)
            Dim pt = vbGetMaxDist(rc.mask)
            rc.maxDist = New cv.Point(pt.X + rc.rect.X, pt.Y + rc.rect.Y)
            sortedCells.Add(rc.pixels, rc)
        Next

        If heartBeat() Then labels(2) = "CV_8U format - " + CStr(classCount) + " cells were identified."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = FloodCell_Close(cPtr)
    End Sub
End Class








Public Class RedColor_Binarize : Inherits VB_Algorithm
    Dim binarize As New Binarize_FourWay
    Dim rMin As New RedColor_Basics
    Public Sub New()
        labels(3) = "A 4-way split of the input grayscale image based on brightness"
        desc = "Use RedCloud on a 4-way split based on light to dark in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binarize.Run(src)
        dst3 = vbPalette(binarize.dst2 * 255 / 5)

        rMin.Run(binarize.dst2)
        dst2 = rMin.dst3
        If standalone Or showIntermediate() Then identifyCells(rMin.redCells, rMin.showMaxIndex)
        labels(2) = rMin.labels(3)
    End Sub
End Class








' https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class RedColor_CComp : Inherits VB_Algorithm
    Dim ccomp As New CComp_Both
    Dim rMin As New RedColor_Basics
    Public Sub New()
        desc = "Identify each Connected component as a RedCloud Cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        ccomp.Run(src)
        dst3 = vbNormalize32f(ccomp.dst1)
        labels(3) = ccomp.labels(2)

        rMin.Run(dst3)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)
    End Sub
End Class








Public Class RedColor_InputColor : Inherits VB_Algorithm
    Public rMin As New RedColor_Basics
    Dim color As New Color_Basics
    Public Sub New()
        desc = "Floodfill the transformed color output and create cells to be tracked."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        color.Run(src)
        rMin.Run(color.dst2)

        dst2 = rMin.dst2
        dst3 = rMin.dst3
        labels(2) = rMin.labels(2)
        labels(3) = rMin.labels(3)
    End Sub
End Class





Public Class RedColor_LeftRight : Inherits VB_Algorithm
    Dim fCellsLeft As New RedColor_InputColor
    Dim fCellsRight As New RedColor_InputColor
    Public Sub New()
        redOptions.Reduction_Basics.Checked = True
        desc = "Floodfill left and right images after RedCloud color input reduction."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fCellsLeft.Run(task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst2 = fCellsLeft.dst3
        labels(2) = fCellsLeft.rMin.labels(3)

        fCellsRight.Run(task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        dst3 = fCellsRight.dst3
        labels(3) = fCellsRight.rMin.labels(3)
    End Sub
End Class







Public Class RedColor_Histogram3DBP : Inherits VB_Algorithm
    Dim colorC As New RedColor_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Public Sub New()
        desc = "Use the backprojection of the 3D RGB histogram as input to RedColor_Basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hColor.Run(src)
        dst2 = hColor.dst3
        labels(2) = hColor.labels(3)

        colorC.Run(dst2)
        dst3 = colorC.dst2
        dst3.SetTo(0, task.noDepthMask)
        labels(3) = colorC.labels(2)
    End Sub
End Class








Public Class RedColor_Cells : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public cellmap As New cv.Mat
    Public redCells As New List(Of rcData)
    Public Sub New()
        redOptions.UseColor.Checked = True
        desc = "Create RedCloud output using only color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        cellmap = redC.cellMap
        redCells = redC.redCells
    End Sub
End Class








Public Class RedColor_Flippers : Inherits VB_Algorithm
    Dim binarize As New Binarize_FourWay
    Dim rMin As New RedColor_Basics
    Public Sub New()
        redOptions.DesiredCellSlider.Value = 100
        labels(3) = "Highlighted below are the cells which flipped in color from the previous frame."
        desc = "Identify the 4-way split cells that are flipping between brightness boundaries."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binarize.Run(src)

        rMin.Run(binarize.dst2)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        Static lastMap As cv.Mat = rMin.dst3.Clone
        dst3.SetTo(0)
        Dim unMatched As Integer
        Dim unMatchedPixels As Integer
        For Each cell In rMin.redCells
            Dim lastColor = lastMap.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            If lastColor <> cell.color Then
                dst3(cell.rect).SetTo(cell.color, cell.mask)
                unMatched += 1
                unMatchedPixels += cell.pixels
            End If
        Next
        lastMap = rMin.dst3.Clone

        If standalone Or showIntermediate() Then identifyCells(rMin.redCells, rMin.showMaxIndex)

        If heartBeat() Then
            labels(3) = "Unmatched to previous frame: " + CStr(unMatched) + " totaling " + CStr(unMatchedPixels) + " pixels."
        End If
    End Sub
End Class