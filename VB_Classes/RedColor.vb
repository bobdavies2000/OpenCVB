Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedColor_Basics : Inherits VB_Algorithm
    Dim binarize As New Binarize_FourWay
    Dim rMin As New RedMin_Basics
    Public Sub New()
        labels(3) = "A 4-way split of the input grayscale image based on brightness"
        desc = "Use RedCloud on a 4-way split based on light to dark in the image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binarize.Run(src)
        dst3 = vbPalette(binarize.dst2 * 255 / 5)

        rMin.Run(binarize.dst2)
        dst2 = rMin.dst3
        If standalone Or showIntermediate() Then identifyCells(rMin.minCells, rMin.showMaxIndex)
        labels(2) = rMin.labels(3)
    End Sub
End Class






Public Class RedColor_BasicsOld : Inherits VB_Algorithm
    Dim redCore As New RedColor_Core
    Public redCells As New List(Of rcData)
    Dim lastMap As cv.Mat
    Public Sub New()
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        lastMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels(3) = "The colors are unstable because there is no cell matching to the previous generation."
        desc = "Match redCells from the current generation to the last."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim lastCells As New List(Of rcData)(redCore.redCells)
        Dim lastMap = dst3.Clone

        redCore.Run(src)

        Dim ftmp As New List(Of rcData)
        Dim lrc As rcData
        Dim usedColors1 As New List(Of cv.Vec3b)
        For Each rc In redCore.redCells
            Dim prev = lastMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If prev < lastCells.Count And prev <> 0 Then
                lrc = lastCells(prev)
                rc.indexLast = lrc.index
                rc.color = lrc.color
                rc.maxDStable = lrc.maxDStable
                Dim stableCheck = lastMap.Get(Of Byte)(lrc.maxDStable.Y, lrc.maxDStable.X)
                If stableCheck = rc.indexLast Then rc.maxDStable = lrc.maxDStable ' keep maxDStable if cell matched to previous
            End If
            If usedColors1.Contains(rc.color) Then
                rc.color = randomCellColor()
            End If
            usedColors1.Add(rc.color)
            ftmp.Add(rc)
        Next

        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors2 As New List(Of cv.Vec3b)
        For Each rc In ftmp
            If usedColors2.Contains(rc.color) Then rc.color = randomCellColor()
            dst3(rc.rect).SetTo(rc.color, rc.mask)
            dst2(rc.rect).SetTo(rc.index, rc.mask)
        Next

        redCells = New List(Of rcData)(ftmp)
        setSelectedCell(redCells, dst2)
        showSelectedCell(dst2)
        labels(2) = redCore.labels(2)
    End Sub
End Class








Public Class RedColor_Core : Inherits VB_Algorithm
    Public classCount As Integer
    Public redCells As New List(Of rcData)
    Dim color As New Color_Basics
    Public Sub New()
        cPtr = FloodCell_Open()
        gOptions.PixelDiffThreshold.Value = 0
        desc = "Floodfill an image so each cell can be tracked.  NOTE: cells are not matched to previous image.  Use RedMin_Basics for matching."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        color.Run(src)
        src = color.dst2

        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = FloodCell_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols, src.Type,
                                     redOptions.DesiredCellSlider.Value, gOptions.PixelDiffThreshold.Value)
        handleInput.Free()

        dst2 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr)

        classCount = FloodCell_Count(cPtr)
        If heartBeat() Then labels(3) = CStr(classCount) + " cells found"
        If classCount <= 1 Then Exit Sub

        Dim sizeData = New cv.Mat(classCount, 1, cv.MatType.CV_32S, FloodCell_Sizes(cPtr))
        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, FloodCell_Rects(cPtr))
        Dim depthMean As cv.Scalar, depthStdev As cv.Scalar
        redCells.Clear()
        redCells.Add(New rcData) ' placeholder so index aligns with offset.
        If standalone Or showIntermediate() Then dst3.SetTo(0)
        For i = 0 To classCount - 1
            Dim rc As New rcData
            rc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            rc.pixels = sizeData.Get(Of Integer)(i, 0)
            rc.index = redCells.Count
            rc.mask = dst2(rc.rect).InRange(rc.index, rc.index)
            rc.color = task.vecColors(i) ' never more than 255...
            rc.maxDist = vbGetMaxDist(rc)
            rc.maxDStable = rc.maxDist ' assume it has to use the latest.

            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(rc.mask, rc.contour, 255, -1)

            Dim minLoc As cv.Point, maxLoc As cv.Point
            task.pcSplit(0)(rc.rect).MinMaxLoc(rc.minVec.X, rc.maxVec.X, minLoc, maxLoc, rc.mask)
            task.pcSplit(1)(rc.rect).MinMaxLoc(rc.minVec.Y, rc.maxVec.Y, minLoc, maxLoc, rc.mask)
            task.pcSplit(2)(rc.rect).MinMaxLoc(rc.minVec.Z, rc.maxVec.Z, minLoc, maxLoc, rc.mask)
            cv.Cv2.MeanStdDev(task.pointCloud(rc.rect), depthMean, depthStdev, rc.mask)

            rc.depthMean = New cv.Point3f(depthMean(0), depthMean(1), depthMean(2))
            rc.depthStdev = New cv.Point3f(depthStdev(0), depthStdev(1), depthStdev(2))

            redCells.Add(rc)

            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next

        If standalone Then
            setSelectedCell(redCells, dst2)
            showSelectedCell(dst2)
        End If
        If heartBeat() Then labels(2) = CStr(classCount) + " cells were identified."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = FloodCell_Close(cPtr)
    End Sub
End Class








' https://docs.opencv.org/master/de/d01/samples_2cpp_2connected_components_8cpp-example.html
Public Class RedColor_CComp : Inherits VB_Algorithm
    Dim ccomp As New CComp_Both
    Dim rMin As New RedMin_Basics
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
    Public rMin As New RedMin_Basics
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
    Dim rMin As New RedMin_Basics
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
        For Each cell In rMin.minCells
            Dim lastColor = lastMap.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            If lastColor <> cell.color Then
                dst3(cell.rect).SetTo(cell.color, cell.mask)
                unMatched += 1
                unMatchedPixels += cell.pixels
            End If
        Next
        lastMap = rMin.dst3.Clone

        If standalone Or showIntermediate() Then identifyCells(rMin.minCells, rMin.showMaxIndex)

        If task.cellSelect.index <> 0 Then
            dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)
            drawPolkaDot(task.cellSelect.maxDist, dst2)
        End If

        If heartBeat() Then
            labels(3) = "Unmatched to previous frame: " + CStr(unMatched) + " totaling " + CStr(unMatchedPixels) + " pixels."
        End If
    End Sub
End Class