Imports cv = OpenCvSharp
Public Class Flood_Basics : Inherits TaskParent
    Public Sub New()
        task.redC = New RedCloud_Basics
        desc = "Build the RedCloud cells with the grayscale input."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If src.Channels = 1 Then task.redC.inputMask = src
        task.redC.Run(src)
        dst2 = task.redC.dst2
        labels = task.redC.labels
    End Sub
End Class





Public Class Flood_CellStatsPlot : Inherits TaskParent
    Public Sub New()
        task.redOptions.DisplayCellStats.Checked = True
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.gOptions.setHistogramBins(1000)
        task.redC = New RedCloud_Basics
        labels(1) = "Histogram of the depth for the selected cell.  Click any cell in the lower left."
        desc = "Provide cell stats on the flood_basics cells.  Identical to Cell_Floodfill"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        task.redC.Run(src)

        dst3 = task.redC.stats.dst1
        dst2 = task.redC.dst2

        If task.ClickPoint = newPoint Then
            If task.redCells.Count > 1 Then
                task.rc = task.redCells(1)
                task.ClickPoint = task.rc.maxDist
            End If
        End If
    End Sub
End Class








Public Class Flood_ContainedCells : Inherits TaskParent
    Public Sub New()
        desc = "Find cells that have only one neighbor.  They are likely to be contained in another cell."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If standalone Then
            If task.firstPass Then task.redC = New RedCloud_Basics
            task.redC.Run(src)
            dst2 = task.redC.dst2
            labels = task.redC.labels
        End If

        Dim removeCells As New List(Of Integer)
        For i = task.redCells.Count - 1 To task.redOptions.identifyCount Step -1
            Dim rc = task.redCells(i)
            Dim nabs As New List(Of Integer)
            Dim contains As New List(Of Integer)
            Dim count = Math.Min(task.redOptions.identifyCount, task.redCells.Count)
            For j = 0 To count - 1
                Dim rcBig = task.redCells(j)
                If rcBig.rect.IntersectsWith(rc.rect) Then nabs.Add(rcBig.index)
                If rcBig.rect.Contains(rc.rect) Then contains.Add(rcBig.index)
            Next
            If contains.Count = 1 Then removeCells.Add(rc.index)
        Next

        dst3.SetTo(0)
        For Each index In removeCells
            Dim rc = task.redCells(index)
            dst3(rc.rect).SetTo(rc.color, rc.mask)
        Next

        If task.heartBeat Then
            labels(3) = CStr(removeCells.Count) + " cells were completely contained in another cell's rect"
        End If
    End Sub
End Class







Public Class Flood_BasicsMask : Inherits TaskParent
    Public binarizedImage As cv.Mat
    Public inputMask As cv.Mat
    Public cellGen As New Cell_Generate
    Dim redCPP As New RedCloud_CPP
    Public buildInputMask As Boolean
    Public showSelected As Boolean = True
    Dim color8U As New Color8U_Basics
    Public Sub New()
        labels(3) = "The inputMask used to limit how much of the image is processed."
        desc = "Floodfill by color as usual but this is run repeatedly with the different tiers."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If standalone Or buildInputMask Then
            color8U.Run(src)
            inputMask = task.pcSplit(2).InRange(task.MaxZmeters, task.MaxZmeters).ConvertScaleAbs()
            src = color8U.dst2
        End If

        dst3 = inputMask
        redCPP.inputMask = inputMask
        redCPP.Run(src)

        cellGen.classCount = redCPP.classCount
        cellGen.rectList = redCPP.rectList
        cellGen.floodPoints = redCPP.floodPoints
        cellGen.Run(redCPP.dst2)

        dst2 = cellGen.dst2

        Dim cellCount = Math.Min(task.redOptions.identifyCount, task.redCells.Count)
        If task.heartBeat Then labels(2) = $"{task.redCells.Count} cells identified and the largest {cellCount} are numbered below."

        If showSelected Then task.setSelectedCell()
    End Sub
End Class





Public Class Flood_Tiers : Inherits TaskParent
    Dim flood As New Flood_BasicsMask
    Dim tiers As New Depth_Tiers
    Dim color8U As New Color8U_Basics
    Public Sub New()
        desc = "Subdivide the Flood_Basics cells using depth tiers."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        Dim tier = task.gOptions.DebugSliderValue

        tiers.Run(src)
        If tier >= tiers.classCount Then tier = 0

        If tier = 0 Then
            dst1 = Not tiers.dst2.InRange(0, 1)
        Else
            dst1 = Not tiers.dst2.InRange(tier, tier)
        End If

        labels(2) = tiers.labels(2) + " in tier " + CStr(tier) + ".  Use the global options 'DebugSlider' to select different tiers."

        color8U.Run(src)

        flood.inputMask = dst1
        flood.Run(color8U.dst2)

        dst2 = flood.dst2
        dst3 = flood.dst3

        task.setSelectedCell()
    End Sub
End Class







Public Class Flood_Motion : Inherits TaskParent
    Dim flood As New Flood_Basics
    Dim redCells As New List(Of rcData)
    Dim cellMap As New cv.Mat
    Dim maxDists As New List(Of cv.Point2f)
    Dim maxIndex As New List(Of Integer)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Create RedCloud cells every heartbeat and compare the results against RedCloud cells created with the current frame."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If task.heartBeat Then
            flood.Run(src)
            redCells = New List(Of rcData)(task.redCells)
            cellMap = task.redMap.Clone
            dst2 = flood.dst2.Clone
            dst3 = flood.dst2.Clone
            labels(2) = flood.labels(2)
            labels(3) = flood.labels(2)

            maxDists.Clear()
            For Each rc In redCells
                maxDists.Add(rc.maxDist)
                maxIndex.Add(rc.index)
            Next
        Else
            flood.Run(src)
            dst1.SetTo(0)
            For i = 0 To task.redCells.Count - 1
                Dim rc = task.redCells(i)
                If maxDists.Contains(rc.maxDist) Then
                    Dim lrc = redCells(maxIndex(maxDists.IndexOf(rc.maxDist)))
                    dst1(lrc.rect).SetTo(lrc.color, lrc.mask)
                End If
            Next
            dst3 = flood.dst2
            labels(3) = flood.labels(2)
        End If
    End Sub
End Class






Public Class Flood_Motion1 : Inherits TaskParent
    Dim flood As New Flood_Basics
    Dim motion As New Motion_Basics
    Dim redCells As New List(Of rcData)
    Dim maxDists As New List(Of cv.Point2f)
    Dim maxIndex As New List(Of Integer)
    Public Sub New()
        desc = "Create RedCloud cells every heartbeat and compare the results against RedCloud cells created with the current frame."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If task.heartBeat Then
            flood.Run(src)
            redCells = New List(Of rcData)(task.redCells)
            dst2 = flood.dst2.Clone
            dst3 = flood.dst2.Clone
            labels(2) = flood.labels(2)
            labels(3) = flood.labels(2)

            maxDists.Clear()
            For Each rc In redCells
                maxDists.Add(rc.maxDist)
                maxIndex.Add(rc.index)
            Next
        Else
            flood.Run(src)
            motion.Run(flood.dst2)

            For i = 0 To task.redCells.Count - 1
                Dim rc = task.redCells(i)
                If maxDists.Contains(rc.maxDist) Then
                    Dim lrc = redCells(maxIndex(maxDists.IndexOf(rc.maxDist)))
                    dst1(lrc.rect).SetTo(lrc.color, lrc.mask)
                End If
            Next
            dst3 = flood.dst2
            labels(3) = flood.labels(2)
        End If
    End Sub
End Class







Public Class Flood_MaxDistPoints : Inherits TaskParent
    Dim redCPP As New RedCloud_MaxDist_CPP
    Public cellGen As New Cell_Generate
    Dim color8U As New Color8U_Basics
    Public Sub New()
        labels(3) = "Contour boundaries - input to RedCloud_Basics"
        desc = "Build the RedCloud cells by providing the maxDist floodpoints to the RedCell C++ code."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        color8U.Run(src)
        redCPP.Run(color8U.dst2)
        If redCPP.classCount = 0 Then Exit Sub ' no data to process.

        cellGen.classCount = redCPP.classCount
        cellGen.rectList = redCPP.RectList
        cellGen.floodPoints = redCPP.floodPoints
        cellGen.removeContour = False
        cellGen.Run(redCPP.dst2)

        dst2 = cellGen.dst2

        redCPP.maxList.Clear()
        For i = 1 To task.redCells.Count - 1
            redCPP.maxList.Add(task.redCells(i).maxDist.X)
            redCPP.maxList.Add(task.redCells(i).maxDist.Y)
        Next

        task.setSelectedCell()

        labels(2) = cellGen.labels(2)
    End Sub
End Class
