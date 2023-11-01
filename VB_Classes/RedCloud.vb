Imports cv = OpenCvSharp
Public Class RedBP_Basics : Inherits VB_Algorithm
    Public prepCells As New List(Of rcPrep)
    Dim matchCell As New RedBP_MatchCell
    Public combine As New RedCloud_InputCombined
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Match cells from the previous generation"
    End Sub
    Public Function redSelect(ByRef dstInput0 As cv.Mat, ByRef dstInput1 As cv.Mat, ByRef dstInput2 As cv.Mat) As rcData
        If task.drawRect <> New cv.Rect Then Return New rcData
        If task.redCells.Count = 0 Then Return New rcData
        If task.clickPoint = New cv.Point(0, 0) Then Return New rcData
        Dim index = task.cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)

        Dim rc = task.redCells(index)
        If task.mouseClickFlag Or heartBeat() Then
            rc.maxDStable = rc.maxDist
            task.redCells(index) = rc
        End If

        If rc.index = task.redOther Then
            rc.maxDist = task.clickPoint
            rc.maxDStable = rc.maxDist
            Dim gridID = task.gridToRoiIndex.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
            rc.rect = task.gridList(gridID)
            rc.mask = task.cellMap(rc.rect).InRange(task.redOther, task.redOther)
        End If

        dstInput0.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        dstInput0(rc.rect).SetTo(cv.Scalar.White, rc.mask)

        dstInput1.Rectangle(rc.rect, cv.Scalar.Yellow, task.lineWidth)
        dstInput1(rc.rect).SetTo(cv.Scalar.White, rc.mask)

        dstInput2(rc.rect).SetTo(cv.Scalar.White, rc.mask)
        dstInput2.Circle(rc.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)

        dstInput2.Circle(rc.maxDStable, task.dotSize + 2, cv.Scalar.Black, -1, task.lineType)
        dstInput2.Circle(rc.maxDStable, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Return rc
    End Function
    Public Sub RunVB(src As cv.Mat)
        task.redOther = 0

        dst0 = src
        If standalone Then dst1 = task.depthRGB.Resize(src.Size)

        combine.Run(src)
        prepCells = combine.prepCells

        If firstPass Then
            task.cellMap.SetTo(task.redOther)
            task.lastCells.Clear()
        End If

        matchCell.lastCellMap = task.cellMap.Clone
        task.lastCells = New List(Of rcData)(task.redCells)
        matchCell.usedColors.Clear()
        matchCell.usedColors.Add(black)

        If heartBeat() Then matchCell.dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC3, 0)

        If dst2.Size <> src.Size Then dst2 = New cv.Mat(src.Size, cv.MatType.CV_8UC3, 0)
        task.cellMap.SetTo(task.redOther)

        task.redCells.Clear()
        task.redCells.Add(New rcData)
        Dim spotsRemoved As Integer
        For Each rp In prepCells
            rp.maxDist = vbGetMaxDist(rp)
            Dim spotTakenTest = task.cellMap.Get(Of Byte)(rp.maxDist.Y, rp.maxDist.X)
            If spotTakenTest <> task.redOther Then
                spotsRemoved += 1
                Continue For
            End If

            rp.index = task.redCells.Count
            matchCell.rp = rp
            matchCell.Run(Nothing)

            If matchCell.rc.pixels < task.minPixels Then
                task.cellMap(matchCell.rc.rect).SetTo(task.redOther, matchCell.rc.mask)
                Continue For
            End If
            task.redCells.Add(matchCell.rc)

            If task.redCells.Count >= 255 Then Exit For ' we are going to handle only the largest 255 cells - "Other" (zero) for the rest.
        Next

        Dim unMatchedCells As Integer
        For i = task.redCells.Count - 1 To 0 Step -1
            Dim rc = task.redCells(i)
            Dim valMax = task.cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            ' if the maxdist has been overlaid by another cell, then correct it here.
            If valMax <> rc.index Then task.cellMap.Set(Of Byte)(rc.maxDist.Y, rc.maxDist.X, rc.index)

            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
            dst2(rc.rect).SetTo(rc.color, rc.mask)

            If rc.indexLast = 0 Then unMatchedCells += 1
        Next

        Static changedTotal As Integer
        changedTotal += unMatchedCells
        labels(3) = CStr(changedTotal) + " new/moved cells in the last second " +
                    Format(changedTotal / (task.frameCount - task.toggleFrame), fmt1) + " unmatched per frame"
        If heartBeat() Then
            labels(2) = CStr(task.redCells.Count) + " cells " + CStr(unMatchedCells) + " did not match the previous frame. Click a cell to see more."
            changedTotal = 0
        End If

        task.rcSelect = redSelect(dst0, dst1, dst2)
        dst3 = matchCell.dst3
    End Sub
End Class









Public Class RedCloud_CloudOnly : Inherits VB_Algorithm
    Dim fCell As New RedColor_Basics
    Dim prep As New RedCloud_Core
    Public Sub New()
        gOptions.useHistoryCloud.Checked = False ' no artifacts.
        desc = "Run RedColor_Basics only on the prep'd data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        prep.Run(Nothing)

        fCell.Run(prep.dst2)

        dst2 = fCell.dst2
        dst2.SetTo(0, task.noDepthMask)
        labels(2) = fCell.labels(2)
    End Sub
End Class








Public Class RedCloud_Core : Inherits VB_Algorithm
    Public Sub New()
        redOptions.EnableAllChannels(True)
        desc = "Reduction transform for the point cloud"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim reduceAmt = redOptions.PCreductionSlider.Value
        task.pointCloud.ConvertTo(dst0, cv.MatType.CV_32S, 1000 / reduceAmt)

        Dim split = dst0.Split()

        Select Case redOptions.PCReduction
            Case "X Reduction"
                dst0 = split(0) * reduceAmt
            Case "Y Reduction"
                dst0 = split(1) * reduceAmt
            Case "Z Reduction"
                dst0 = split(2) * reduceAmt
            Case "XY Reduction"
                dst0 = split(0) * reduceAmt + split(1) * reduceAmt
            Case "XZ Reduction"
                dst0 = split(0) * reduceAmt + split(2) * reduceAmt
            Case "YZ Reduction"
                dst0 = split(1) * reduceAmt + split(2) * reduceAmt
            Case "XYZ Reduction"
                dst0 = split(0) * reduceAmt + split(1) * reduceAmt + split(2) * reduceAmt
        End Select

        Dim mm = vbMinMax(dst0)
        dst2 = (dst0 - mm.minVal)

        dst2.SetTo(mm.maxVal - mm.minVal, task.maxDepthMask)
        dst2.SetTo(0, task.noDepthMask)
        mm = vbMinMax(dst2)
        dst2 *= 254 / mm.maxVal
        dst2 += 1
        dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        labels(2) = "Reduced Pointcloud - reduction factor = " + CStr(reduceAmt)
    End Sub
End Class







Public Class RedCloud_Test : Inherits VB_Algorithm
    Dim prep As New RedCloud_Core
    Public fCell As New RedColor_Basics
    Dim reduction As New Reduction_Basics
    Public Sub New()
        gOptions.HistBinSlider.Value = 20
        desc = "Segment the image based on both the reduced point cloud and color"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        prep.Run(Nothing)
        prep.dst2.ConvertScaleAbs().CopyTo(reduction.dst2, task.depthMask)

        fCell.Run(reduction.dst2)

        dst2 = fCell.dst2
        dst3 = fCell.dst3

        If heartBeat() Then labels(2) = CStr(task.fCells.Count) + " regions identified"
    End Sub
End Class









Public Class RedCloud_InputCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Core
    Public guided As New GuidedBP_Depth
    Public Sub New()
        desc = "Build the reduced pointcloud or doctored back projection input to RedCloud/RedCell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Select Case redOptions.depthInput
            Case "GuidedBP_Depth"
                guided.Run(src)
                Dim maskOfDepth = guided.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
                dst2 = guided.dst2
            Case "RedCloud_Core"
                redC.Run(src)
                dst2 = redC.dst2
            Case "No Pointcloud Data"
                redC.Run(src)
                dst2 = redC.dst2
        End Select
    End Sub
End Class








Public Class RedCloud_InputCombined : Inherits VB_Algorithm
    Dim color As New Color_Basics
    Dim cloud As New RedCloud_InputCloud
    Dim redP As New RedBP_Flood_CPP
    Public prepCells As New List(Of rcPrep)
    Public Sub New()
        desc = "Combined the color and cloud as indicated in the RedOptions panel."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If redOptions.colorInput = "No Color Input" Then
            dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Else
            color.Run(src)
            dst2 = color.dst2
        End If

        If redOptions.depthInput <> "No Pointcloud Data" Then
            cloud.Run(src)
            cloud.dst2.CopyTo(dst2, task.depthMask)
        End If

        redP.Run(dst2)
        dst2 = redP.dst2

        prepCells.Clear()
        For Each key In redP.prepCells
            Dim rp = key.Value
            If task.drawRect <> New cv.Rect Then
                If task.drawRect.Contains(rp.floodPoint) = False Then Continue For
            End If

            prepCells.Add(rp)
        Next
    End Sub
End Class
