Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Flood_Basics : Inherits VB_Algorithm
    Public redCells As New List(Of rcDataNew)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim bestBound As New Boundary_RemovedRects
    Dim flood As New Flood_Split4
    Public Sub New()
        labels(3) = "Contour boundaries - input to Flood_Split4"
        desc = "Build the RedCloud cells with the best boundaries"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        bestBound.Run(src)
        dst1 = bestBound.dst2

        dst3 = bestBound.bRects.bounds.colorC.dst2
        dst3 = dst3 Or dst1

        flood.genCells.removeContour = False
        flood.genCells.cellLimit = bestBound.bRects.bounds.rects.Count - bestBound.bRects.smallRects.Count
        flood.Run(dst3)

        redCells = flood.redCells
        cellMap = flood.cellMap
        dst2 = flood.dst2

        setSelectedContour(redCells, cellMap)
        identifyCells(redCells)

        labels(2) = flood.labels(2)
    End Sub
End Class







Public Class Flood_BasicsMask : Inherits VB_Algorithm
    Public redCells As New List(Of rcDataNew)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public binarizedImage As cv.Mat
    Public inputMask As cv.Mat
    Dim genCells As New RedCloud_GenCells
    Dim redCPP As New RedCloud_Mask_CPP
    Public buildInputMask As Boolean
    Public Sub New()
        labels(3) = "The inputMask used to limit how much of the image is processed."
        desc = "Floodfill by color as usual but this is run repeatedly with the different tiers."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Or buildInputMask Then
            Static colorC As New Color_Basics
            colorC.Run(src)
            inputMask = task.maxDepthMask
            src = colorC.dst2
        End If
        dst3 = inputMask
        redCPP.inputMask = inputMask
        redCPP.Run(src)

        genCells.classCount = redCPP.classCount
        genCells.rectData = redCPP.rectData
        genCells.floodPointData = redCPP.floodPointData
        genCells.sizeData = redCPP.sizeData
        genCells.Run(redCPP.dst2)

        dst2 = genCells.dst2
        cellMap = genCells.dst3
        redCells = genCells.redCells

        Dim cellCount = Math.Min(redOptions.identifyCount, redCells.Count)
        If task.heartBeat Then labels(2) = $"{redCells.Count} cells identified and the largest {cellCount} are numbered below.  " +
                                            "Use the DebugSlider to show different depth tiers."
        setSelectedContour(redCells, cellMap)
        identifyCells(redCells)
    End Sub
End Class








Public Class Flood_Split4 : Inherits VB_Algorithm
    Public redCells As New List(Of rcDataNew)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim binar4 As New Binarize_Split4
    Public genCells As New RedCloud_GenCells
    Dim redCPP As New RedCloud_MaskNone_CPP
    Public Sub New()
        vbAddAdvice(traceName + ": redOptions 'Desired RedCloud Cells' determines how many regions are isolated.")
        desc = "Simple Floodfill each region but prepare the mask, rect, floodpoints, and pixel counts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binar4.Run(task.color) ' always run split4 to get colors for genCells.
        If src.Channels = 1 Then src += binar4.dst2 Else src = binar4.dst2

        redCPP.Run(src)

        If redCPP.classCount = 0 Then Exit Sub ' no data to process.
        genCells.classCount = redCPP.classCount
        genCells.rectData = redCPP.rectData
        genCells.floodPointData = redCPP.floodPointData
        genCells.sizeData = redCPP.sizeData
        genCells.Run(redCPP.dst2)

        dst2 = genCells.dst2
        cellMap = genCells.dst3
        redCells = genCells.redCells

        Dim cellCount = Math.Min(redOptions.identifyCount, redCells.Count)
        If task.heartBeat Then labels(2) = $"{redCells.Count} cells identified and the largest {cellCount} are numbered below."

        setSelectedContour(redCells, cellMap)
        identifyCells(redCells)
    End Sub
End Class










Public Class Flood_Stats : Inherits VB_Algorithm
    Dim flood As New Flood_Basics
    Dim stats As New Cell_BasicsNew
    Public Sub New()
        desc = "Provide cell stats on the flood_basics cells.  Identical to Cell_Floodfill"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)

        stats.Run(src)
        dst0 = stats.dst0
        dst1 = stats.dst1
        dst2 = flood.dst2
        setTrueText(stats.strOut, 3)
    End Sub
End Class








Public Class Flood_ContainedCells : Inherits VB_Algorithm
    Dim flood As New Flood_Basics
    Public redCells As New List(Of rcDataNew)
    Public Sub New()
        desc = "Find cells that have only one neighbor.  They are likely to be completely contained in another cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            flood.Run(src)
            dst2 = flood.dst2
            redCells = flood.redCells
            labels = flood.labels
        End If

        Dim removeCells As New List(Of Integer)
        For i = redCells.Count - 1 To redOptions.identifyCount Step -1
            Dim rc = redCells(i)
            Dim nabs As New List(Of Integer)
            Dim contains As New List(Of Integer)
            Dim count = Math.Min(redOptions.identifyCount, redCells.Count)
            For j = 0 To count - 1
                Dim rcBig = redCells(j)
                If rcBig.rect.IntersectsWith(rc.rect) Then nabs.Add(rcBig.index)
                If rcBig.rect.Contains(rc.rect) Then contains.Add(rcBig.index)
            Next
            If contains.Count = 1 Then removeCells.Add(rc.index)
        Next

        dst3.SetTo(0)
        For Each index In removeCells
            Dim rc = redCells(index)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next
        identifyCells(redCells)

        If task.heartBeat Then labels(3) = CStr(removeCells.Count) + " cells were completely contained in exactly one other cell's rect"
    End Sub
End Class





Public Class Flood_Tiers : Inherits VB_Algorithm
    Dim flood As New Flood_Basics
    Dim tiers As New Contour_DepthTiers
    Dim plot As New Plot_Histogram
    Public redCells As New List(Of rcDataOld)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        plot.removeZeroEntry = False
        desc = "Subdivide the Flood_Basics cells using depth tiers."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2

        tiers.Run(src)

        redCells.Clear()
        cellMap.SetTo(0)
        Dim ranges = {New cv.Rangef(-1, tiers.classCount + 1)}
        For Each rc In flood.redCells
            'rc.depthMask = rc.mask And task.depthMask(rc.rect)
            'If rc.depthMask.CountNonZero Then
            '    cv.Cv2.CalcHist({tiers.dst2(rc.rect)}, {0}, New cv.Mat, rc.tierHist, 1, {tiers.classCount}, ranges)

            '    ReDim rc.tierHistArray(tiers.classCount - 1)
            '    Dim samples(rc.tierHist.Total - 1) As Single
            '    Marshal.Copy(rc.tierHist.Data, rc.tierHistArray, 0, rc.tierHistArray.Length)
            'End If
            'cellMap(rc.rect).SetTo(rc.index, rc.mask)
            'redCells.Add(rc)
        Next

        If task.rc.tierHist.Rows > 0 Then
            plot.Run(task.rc.tierHist)
            dst3 = plot.dst3
        End If

        setSelectedContour(redCells, cellMap)
        identifyCells(redCells)
    End Sub
End Class