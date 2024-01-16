Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedColor_Basics : Inherits VB_Algorithm
    Public redCore As New RedCloud_CPP
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
        redCore.Run(src)
        Dim lastCells As New List(Of rcData)(redCells)

        redCells.Clear()
        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Vec3b)({black})
        Dim unmatched As Integer
        For Each key In redCore.sortedCells
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
        labels(2) = redCore.labels(3) + " " + CStr(unmatched) + " cells were not matched to previous frame."
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