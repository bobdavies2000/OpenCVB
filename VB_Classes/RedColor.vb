Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedColor_Basics : Inherits VB_Algorithm
    Public redCore As New RedCloud_CPP
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim lastColors As cv.Mat
    Dim lastMap As cv.Mat = dst2.Clone
    Public Sub New()
        lastColors = dst3.Clone
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redCore.Run(src)
        Dim lastCells As New List(Of rcData)(redCells)

        redCells.Clear()
        cellMap.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Vec3b)({black})
        Dim unmatched As Integer
        For Each key In redCore.sortedCells
            Dim cell = key.Value
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)
            If index < lastCells.Count Then
                cell.color = lastColors.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
                ' cell.maxDist = lastCells(index).maxDist
            Else
                unmatched += 1
            End If
            If usedColors.Contains(cell.color) Then
                unmatched += 1
                cell.color = randomCellColor()
            End If
            usedColors.Add(cell.color)

            If cellMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = redCells.Count
                redCells.Add(cell)
                cellMap(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)
            End If
        Next

        If standalone Or showIntermediate() Then identifyCells(redCells)

        If standalone And redCells.Count > 0 Then setSelectedCell(redCells, cellMap)

        labels(3) = CStr(redCells.Count) + " cells were identified.  The top " + CStr(identifyCount) + " are numbered"
        labels(2) = redCore.labels(3) + " " + CStr(unmatched) + " cells were not matched to previous frame."

        lastColors = dst3.Clone
        dst2 = cellMap.Clone
        lastMap = cellMap.Clone
        If redCells.Count > 0 Then dst1 = vbPalette(lastMap * 255 / redCells.Count)
    End Sub
End Class
