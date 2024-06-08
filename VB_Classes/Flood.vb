Imports cv = OpenCvSharp
Public Class Flood_Basics : Inherits VB_Parent
    Dim redCPP As New RedCloud_CPP
    Public genCells As New Cell_Generate
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = True
        desc = "Build the RedCloud cells with the grayscale input."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then src = bgr2gray(src) Else redCPP.inputMask = src
        redCPP.Run(src)

        If redCPP.classCount = 0 Then Exit Sub ' no data to process.

        genCells.classCount = redCPP.classCount
        genCells.rectList = redCPP.rectList
        genCells.floodPoints = redCPP.floodPoints
        genCells.removeContour = False
        genCells.Run(redCPP.dst2)

        dst2 = genCells.dst2

        setSelectedContour()

        labels(2) = genCells.labels(2)
    End Sub
End Class









Public Class Flood_CellStatsPlot : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Dim stats As New Cell_BasicsPlot
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = True
        If standaloneTest() Then task.gOptions.setDisplay1()
        task.gOptions.setHistogramBins(1000)
        labels(1) = "Histogram of the depth for the selected cell.  Click any cell in the lower left."
        desc = "Provide cell stats on the flood_basics cells.  Identical to Cell_Floodfill"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)

        stats.Run(src)
        dst1 = stats.dst1
        dst2 = flood.dst2
        setTrueText(stats.strOut, 3)

        If task.clickPoint = New cv.Point Then
            If task.redCells.Count > 1 Then
                task.rc = task.redCells(1)
                task.clickPoint = task.rc.maxDist
            End If
        End If
    End Sub
End Class








Public Class Flood_ContainedCells : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = True
        desc = "Find cells that have only one neighbor.  They are likely to be completely contained in another cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            flood.Run(src)
            dst2 = flood.dst2
            labels = flood.labels
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

        If task.heartBeat Then labels(3) = CStr(removeCells.Count) + " cells were completely contained in exactly one other cell's rect"
    End Sub
End Class







Public Class Flood_BasicsMask : Inherits VB_Parent
    Public binarizedImage As cv.Mat
    Public inputMask As cv.Mat
    Public genCells As New Cell_Generate
    Dim redCPP As New RedCloud_CPP
    Public buildInputMask As Boolean
    Public showSelected As Boolean = True
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = True
        labels(3) = "The inputMask used to limit how much of the image is processed."
        desc = "Floodfill by color as usual but this is run repeatedly with the different tiers."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Or buildInputMask Then
            Static cvt As New Color8U_Basics
            cvt.Run(src)
            inputMask = task.pcSplit(2).InRange(task.maxZmeters, task.maxZmeters).ConvertScaleAbs()
            src = cvt.dst2
        End If

        dst3 = inputMask
        redCPP.inputMask = inputMask
        redCPP.Run(src)

        genCells.classCount = redCPP.classCount
        genCells.rectList = redCPP.rectList
        genCells.floodPoints = redCPP.floodPoints
        genCells.Run(redCPP.dst2)

        dst2 = genCells.dst2

        Dim cellCount = Math.Min(task.redOptions.identifyCount, task.redCells.Count)
        If task.heartBeat Then labels(2) = $"{task.redCells.Count} cells identified and the largest {cellCount} are numbered below."

        If showSelected Then setSelectedContour()
    End Sub
End Class





Public Class Flood_Tiers : Inherits VB_Parent
    Dim flood As New Flood_BasicsMask
    Dim tiers As New Depth_TiersZ
    Dim cvt As New Color8U_Basics
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = True
        desc = "Subdivide the Flood_Basics cells using depth tiers."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim tier = task.gOptions.DebugSlider.Value

        tiers.Run(src)
        If tier >= tiers.classCount Then tier = 0

        If tier = 0 Then
            dst1 = Not tiers.dst2.InRange(0, 1)
        Else
            dst1 = Not tiers.dst2.InRange(tier, tier)
        End If

        labels(2) = tiers.labels(2) + " in tier " + CStr(tier) + ".  Use the global options 'DebugSlider' to select different tiers."

        cvt.Run(src)

        flood.inputMask = dst1
        flood.Run(cvt.dst2)

        dst2 = flood.dst2
        dst3 = flood.dst3

        setSelectedContour()
    End Sub
End Class







Public Class Flood_Motion : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Dim redCells As New List(Of rcData)
    Dim cellMap As New cv.Mat
    Dim maxDists As New List(Of cv.Point2f)
    Dim maxIndex As New List(Of Integer)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Create RedCloud cells every heartbeat and compare the results against RedCloud cells created with the current frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Then
            flood.Run(src)
            redCells = New List(Of rcData)(task.redCells)
            cellMap = task.cellMap.Clone
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






Public Class Flood_Motion1 : Inherits VB_Parent
    Dim flood As New Flood_Basics
    Dim motion As New Motion_Basics
    Dim redCells As New List(Of rcData)
    Dim maxDists As New List(Of cv.Point2f)
    Dim maxIndex As New List(Of Integer)
    Public Sub New()
        desc = "Create RedCloud cells every heartbeat and compare the results against RedCloud cells created with the current frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
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







Public Class Flood_LeftRight : Inherits VB_Parent
    Dim redLeft As New RedCloud_Basics
    Dim redRight As New RedCloud_Basics
    Public mapLeft As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public mapRight As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public cellsLeft As New List(Of rcData)
    Public cellsRight As New List(Of rcData)
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = False
        If standalone Then task.gOptions.setDisplay1()
        desc = "Floodfill left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        task.redCells = New List(Of rcData)(cellsLeft)
        task.cellMap = mapLeft.Clone

        redLeft.genCells.useLeftImage = True
        redLeft.Run(task.leftView)
        labels(2) = redLeft.labels(2)

        dst2 = redLeft.dst2

        cellsLeft = New List(Of rcData)(task.redCells)
        mapLeft = task.cellMap.Clone

        task.redCells = New List(Of rcData)(cellsRight)
        task.cellMap = mapRight.Clone

        redRight.genCells.useLeftImage = False
        redRight.Run(task.rightView)
        labels(3) = redRight.labels(2)

        dst3 = redRight.dst2

        cellsRight = New List(Of rcData)(task.redCells)
        mapRight = task.cellMap.Clone

        If task.redOptions.IdentifyCells.Checked Then
            If task.mousePicTag = 2 Then
                setSelectedContour(cellsLeft, mapLeft)
                task.color(task.rc.rect).SetTo(cv.Scalar.White, task.rc.mask)
            Else
                setSelectedContour(cellsRight, mapRight)
                dst1 = task.rightView
                dst1(task.rc.rect).SetTo(cv.Scalar.White, task.rc.mask)
            End If
        End If
    End Sub
End Class







Public Class Flood_MaxDistPoints : Inherits VB_Parent
    Dim bounds As New Boundary_RemovedRects
    Dim redCPP As New RedCloud_MaxDist_CPP
    Public genCells As New Cell_Generate
    Public Sub New()
        task.redOptions.IdentifyCells.Checked = True
        labels(3) = "Contour boundaries - input to RedCloud_Basics"
        desc = "Build the RedCloud cells by providing the maxDist floodpoints to the RedCell C++ code."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static cvt As New Color8U_Basics
        cvt.Run(src)
        redCPP.Run(cvt.dst2)
        If redCPP.classCount = 0 Then Exit Sub ' no data to process.

        genCells.classCount = redCPP.classCount
        genCells.rectList = redCPP.rectlist
        genCells.floodPoints = redCPP.floodPoints
        genCells.removeContour = False
        genCells.Run(redCPP.dst2)

        dst2 = genCells.dst2

        redCPP.maxList.Clear()
        For i = 1 To task.redCells.Count - 1
            redCPP.maxList.Add(task.redCells(i).maxDist.X)
            redCPP.maxList.Add(task.redCells(i).maxDist.Y)
        Next

        setSelectedContour()

        labels(2) = genCells.labels(2)
    End Sub
End Class