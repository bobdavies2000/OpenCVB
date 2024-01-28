Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Cell_Basics : Inherits VB_Algorithm
    Dim plot As New Histogram_Depth
    Dim pca As New PCA_Basics
    Dim eq As New Plane_Equation
    Public runRedCloud As Boolean
    Public Sub New()
        If standalone Then gOptions.HistBinSlider.Value = 20
        desc = "Display the statistics for the selected cell."
    End Sub
    Public Sub statsString(src As cv.Mat)
        Dim tmp = New cv.Mat(task.rc.mask.Rows, task.rc.mask.Cols, cv.MatType.CV_32F, 0)
        task.pcSplit(2)(task.rc.rect).CopyTo(tmp, task.rc.mask)
        plot.rc = task.rc
        plot.Run(tmp)
        dst1 = plot.dst2

        Dim rc = task.rc

        Dim gridID = task.gridToRoiIndex.Get(Of Integer)(rc.maxDist.Y, rc.maxDist.X)
        strOut = "rc.index = " + CStr(rc.index) + vbTab + " gridID = " + CStr(gridID) + vbCrLf
        strOut += "rc.rect: " + CStr(rc.rect.X) + ", " + CStr(rc.rect.Y) + ", "
        strOut += CStr(rc.rect.Width) + ", " + CStr(rc.rect.Height) + vbTab + "rc.color = " + rc.color.ToString() + vbCrLf
        strOut += "rc.maxDist = " + CStr(rc.maxDist.X) + ", " + CStr(rc.maxDist.Y) + vbCrLf

        strOut += "Cell is marked as depthCell = " + CStr(rc.depthCell) + vbCrLf
        If rc.depthPixels > 0 Then
            strOut += "rc.pixels " + CStr(rc.pixels) + vbTab + "rc.depthPixels = " + CStr(rc.depthPixels) +
                  " or " + Format(rc.depthPixels / rc.pixels, "0%") + " depth " + vbCrLf
        Else
            strOut += "rc.pixels " + CStr(rc.pixels) + " - no depth data" + vbCrLf
        End If

        strOut += "Min/Max/Range: X = " + Format(rc.minVec.X, fmt1) + "/" + Format(rc.maxVec.X, fmt1)
        strOut += "/" + Format(rc.maxVec.X - rc.minVec.X, fmt1) + vbTab

        strOut += "Y = " + Format(rc.minVec.Y, fmt1) + "/" + Format(rc.maxVec.Y, fmt1)
        strOut += "/" + Format(rc.maxVec.Y - rc.minVec.Y, fmt1) + vbTab

        strOut += "Z = " + Format(rc.minVec.Z, fmt2) + "/" + Format(rc.maxVec.Z, fmt2)
        strOut += "/" + Format(rc.maxVec.Z - rc.minVec.Z, fmt2) + vbCrLf + vbCrLf

        strOut += "Cell Mean in 3D: x/y/z = " + vbTab + Format(rc.depthMean.X, fmt2) + vbTab
        strOut += Format(rc.depthMean.Y, fmt2) + vbTab + Format(rc.depthMean.Z, fmt2) + vbCrLf

        If rc.depthMean.Z = 0 Then
            strOut += vbCrLf + "No depth data is available for that cell. "
        Else
            eq.rc = rc
            eq.Run(src)
            rc = eq.rc
            strOut += vbCrLf + eq.strOut + vbCrLf

            pca.Run(empty)
            strOut += vbCrLf + pca.strOut
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Or runRedCloud Then
            Static redC As New RedCloud_Basics
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)
            setSelectedCell(redC.redCells, redC.cellMap)
        End If
        If task.heartBeat Then statsString(src)

        setTrueText(strOut, 3)
        labels(1) = "Histogram plot for the cell's depth data - X-axis varies from 0 to " + CStr(CInt(task.maxZmeters)) + " meters"
    End Sub
End Class








Public Class Cell_PixelCountCompare : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        desc = "The rc.mask is filled and may completely contain depth pixels.  This alg finds cells that contain depth islands."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        Dim missCount As Integer
        For Each rc In redC.redCells
            If rc.depthPixels <> 0 Then
                If rc.pixels <> rc.depthPixels Then
                    dst3(rc.rect).SetTo(rc.color, rc.mask)
                    Dim pt = New cv.Point(rc.maxDist.X - 10, rc.maxDist.Y)
                    If gOptions.DebugCheckBox.Checked Then
                        strOut = CStr(rc.pixels) + ", " + CStr(rc.depthPixels)
                    Else
                        strOut = Format(rc.depthPixels / rc.pixels, "0%")
                    End If
                    setTrueText(strOut, pt, 3)
                    missCount += 1
                End If
            End If
        Next
        If task.heartBeat Then labels(3) = "There were " + CStr(missCount) + " cells that contained an island of depth pixels - value = (pixels, depthpixels)"
    End Sub
End Class










Public Class Cell_Stable : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public rcUnstableList As New List(Of rcData)
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Use maxDStable to identify stable cells - cells which were NOT present in the previous generation."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = dst2.Clone
        dst3(task.rc.rect).SetTo(task.rc.color, task.rc.mask)
        labels(2) = redC.labels(2)

        Static prevList As New List(Of cv.Point)
        If task.heartBeat Or task.frameCount = 2 Then
            prevList.Clear()
            For Each rc In redC.redCells
                prevList.Add(rc.maxDStable)
            Next
        End If

        Dim unstableList As New List(Of rcData)
        For Each rc In redC.redCells
            If prevList.Contains(rc.maxDStable) = False Then
                vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.White, -1)
                vbDrawContour(dst3(rc.rect), rc.contour, cv.Scalar.Black)
                unstableList.Add(rc)
            End If
        Next

        If task.almostHeartBeat Then
            rcUnstableList = New List(Of rcData)(unstableList)
            labels(1) = CStr(rcUnstableList.Count) + " found before the heartbeat."
        End If
        dst1.SetTo(0)
        For Each rc In rcUnstableList
            dst1(rc.rect).SetTo(rc.color, rc.mask)
        Next

        labels(3) = CStr(unstableList.Count) + " cells weren't present in the previous generation."
    End Sub
End Class








Public Class Cell_StableMax : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Highligh cells that were present the max number of match counts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        redCells.Clear()
        dst3.SetTo(0)
        cellMap.SetTo(0)
        dst1.SetTo(255) ' the unstable mask 
        For Each rc In redC.redCells
            If rc.matchCount = task.rcMatchMax Then
                dst3(rc.rect).SetTo(rc.color, rc.mask)
                dst1(rc.rect).SetTo(0)
                cellMap(rc.rect).SetTo(rc.index, rc.mask)
                redCells.Add(rc)
            End If
        Next
        labels(3) = CStr(redCells.Count) + " cells were stable - present since the last heartbeat."
    End Sub
End Class






Public Class Cell_StableColors : Inherits VB_Algorithm
    Dim stable As New Cell_StableMax
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.UseColor.Checked = True
        desc = "Identify cells using only color and find the cells that are stable."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        stable.Run(src)
        dst2 = stable.dst2
        dst3 = stable.dst3
        labels(2) = stable.labels(2)
        labels(3) = stable.labels(3)
    End Sub
End Class







Public Class Cell_ValidateColorCells : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "Cells shown below have rc.depthPixels / rc.pixels < 50%"
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        desc = "Validate that all the depthCells are correctly identified."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst1.SetTo(0)
        dst3.SetTo(0)
        Dim percentDepth As New List(Of Single)
        For Each rc In redC.redCells
            If rc.depthCell = False Then dst1(rc.rect).SetTo(255, rc.mask)
            If rc.depthCell And rc.index > 0 Then
                Dim pc = rc.depthPixels / rc.pixels
                percentDepth.Add(pc)

                If pc < 0.5 Then dst3(rc.rect).SetTo(rc.color, rc.mask)
            End If
        Next

        Dim beforeCount = dst1.CountNonZero
        dst1.SetTo(0, task.depthMask)
        Dim aftercount = dst1.CountNonZero

        If beforeCount <> aftercount Then
            strOut = "There are color cells with depth in them - not good" + vbCrLf
        Else
            strOut = "There are no color cells with depth in them." + vbCrLf
        End If
        If percentDepth.Count > 0 Then
            strOut += "Depth cell percentage average " + Format(percentDepth.Average, "0%") + vbCrLf
            strOut += "Depth cell percentage range " + Format(percentDepth.Min, "0%") + " to " + Format(percentDepth.Max, "0%")
        End If
        setTrueText(strOut, 3)
    End Sub
End Class











Public Class Cell_JumpUp : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public jumpCells As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent jump in size", 1, 100, 25)
        desc = "Identify cells that have jumped up in size since the last frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("Percent jump in size")
        Dim percentJump = percentSlider.value / 100

        Dim lastCells = New List(Of rcData)(redC.redCells)
        redC.Run(src)
        dst2 = redC.dst2
        If task.heartBeat Then dst3.SetTo(0)
        labels(2) = redC.labels(2)

        jumpCells.Clear()
        For Each rc In redC.redCells
            If rc.matchFlag Then
                Dim lrc = lastCells(rc.indexLast)
                If (rc.pixels - lrc.pixels) / rc.pixels >= percentJump Then
                    dst3(lrc.rect).SetTo(cv.Scalar.White, lrc.mask)
                    jumpCells.Add(rc.index, New cv.Vec2i(lrc.index, rc.index))
                End If
            End If
        Next
        If task.heartBeat Then labels(3) = "There were " + CStr(jumpCells.Count) + " cells jumped up more than " +
                                         Format(percentJump, "0%")
        If task.almostHeartBeat Then dst1 = dst3.Clone
    End Sub
End Class










Public Class Cell_JumpDown : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public jumpCells As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent jump in size", 1, 100, 25)
        desc = "Identify cells that have jumped down in size since the last frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("Percent jump in size")
        Dim percentJump = percentSlider.value / 100

        Dim lastCells = New List(Of rcData)(redC.redCells)
        redC.Run(src)
        dst2 = redC.dst2
        If task.heartBeat Then dst3.SetTo(0)
        labels(2) = redC.labels(2)

        jumpCells.Clear()
        For Each rc In redC.redCells
            If rc.matchFlag Then
                Dim lrc = lastCells(rc.indexLast)
                If (lrc.pixels - rc.pixels) / rc.pixels >= percentJump Then
                    dst3(lrc.rect).SetTo(cv.Scalar.White, lrc.mask)
                    jumpCells.Add(rc.index, New cv.Vec2i(lrc.index, rc.index))
                End If
            End If
        Next
        If task.heartBeat Then labels(3) = "There were " + CStr(jumpCells.Count) + " cells jumped down more than " +
                                         Format(percentJump, "0%")
        If task.almostHeartBeat Then dst1 = dst3.Clone
    End Sub
End Class










Public Class Cell_JumpUnstable : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public jumpCells As New SortedList(Of Integer, cv.Vec2i)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Percent jump in size", 1, 100, 25)
        desc = "Identify cells that have changed size more than X% since the last frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static percentSlider = findSlider("Percent jump in size")
        Dim percentJump = percentSlider.value / 100

        If task.heartBeat Or task.midHeartBeat Then
            Dim lastCells As New List(Of rcData)(redC.redCells)
            redC.Run(src)
            dst2 = redC.dst2
            dst3 = dst1.Clone
            dst1.SetTo(0)
            labels(2) = redC.labels(2)

            jumpCells.Clear()
            For Each rc In redC.redCells
                If rc.matchFlag Then
                    Dim lrc = lastCells(rc.indexLast)
                    If Math.Abs(lrc.pixels - rc.pixels) / rc.pixels >= percentJump Then
                        dst1(lrc.rect).SetTo(cv.Scalar.White, lrc.mask)
                        jumpCells.Add(rc.index, New cv.Vec2i(lrc.index, rc.index))
                    End If
                End If
            Next
            labels(3) = "There were " + CStr(jumpCells.Count) + " cells changed more than " + Format(percentJump, "0%") + " up or down"
        End If
    End Sub
End Class






Public Class Cell_Distance : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public redCells As New List(Of rcData)
    Public cellMap As cv.Mat
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels = {"", "Depth distance to selected cell", "", "Color distance to selected cell"}
        desc = "Measure the color distance of each cell to the selected cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Or task.quarterBeat Then
            redC.Run(src)
            dst0 = task.color
            cellMap = redC.cellMap
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            redCells.Clear()
            Dim maxColorDistance As Integer
            Static maxDistance As Integer
            For Each rc In redC.redCells
                rc.colorDistance = CInt(distance3D(task.rc.colorMean, rc.colorMean))
                rc.depthDistance = distance3D(task.rc.depthMean, rc.depthMean)
                redCells.Add(rc)
                If maxColorDistance < rc.colorDistance Then maxColorDistance = rc.colorDistance
            Next

            If maxDistance < maxColorDistance Then maxDistance = maxColorDistance
            If task.heartBeat Then maxDistance = maxColorDistance

            dst1.SetTo(0)
            dst3.SetTo(0)
            For Each rc In redCells
                dst1(rc.rect).SetTo(255 - rc.depthDistance * 255 / task.maxZmeters, rc.mask)
                dst3(rc.rect).SetTo(255 - rc.colorDistance * 255 / maxDistance, rc.mask)
            Next
        End If
    End Sub
End Class








Public Class Cell_Binarize : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels = {"", "Binarized image", "", "Relative gray image"}
        desc = "Separate the image into light and dark using RedCloud cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst0 = src
        If task.heartBeat Or task.quarterBeat Then
            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            Dim grayMeans As New List(Of Single)
            For Each rc In redC.redCells
                Dim grayMean As cv.Scalar, grayStdev As cv.Scalar
                cv.Cv2.MeanStdDev(task.gray(rc.rect), grayMean, grayStdev, rc.mask)
                grayMeans.Add(grayMean(0))
            Next
            Dim min = grayMeans.Min
            Dim max = grayMeans.Max
            Dim avg = grayMeans.Average

            dst3.SetTo(0)
            For Each rc In redC.redCells
                Dim color = (grayMeans(rc.index) - min) * 255 / (max - min)
                dst3(rc.rect).SetTo(color, rc.mask)
                dst1(rc.rect).SetTo(If(grayMeans(rc.index) > avg, 255, 0), rc.mask)
            Next
        End If
    End Sub
End Class











Public Class Cell_DistanceDepth : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public colorOnly As New RedCloud_Cells
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Measure color distance from black for both color and depth cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Or task.quarterBeat Then
            redOptions.UseDepth.Checked = True
            redC.Run(src)
            dst2 = redC.dst2.Clone
            labels(2) = redC.labels(2)

            redOptions.UseColor.Checked = True
            colorOnly.Run(src)
            dst3 = colorOnly.dst2.Clone
            labels(3) = colorOnly.labels(2)
        End If
    End Sub
End Class