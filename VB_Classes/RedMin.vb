Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices






Public Class RedColor_BasicsMotion : Inherits VB_Algorithm
    Public redCore As New RedCloud_CPP
    Public redCells As New List(Of rcData)
    Public rMotion As New RedMin_Motion
    Dim lastColors = dst3.Clone
    Dim lastMap As cv.Mat = dst2.Clone
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "Mask of active RedMin cells", "CV_8U representation of redCells", ""}
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redCore.Run(src)

        rMotion.sortedCells = redCore.sortedCells
        rMotion.Run(task.color.Clone)

        Dim lastCells As New List(Of rcData)(redCells)

        redCells.Clear()
        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Vec3b)({black})
        Dim motionCount As Integer
        For Each cell In rMotion.redCells
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)
            If cell.motionFlag = False Then
                If index > 0 And index < lastCells.Count Then cell = lastCells(index - 1)
            Else
                motionCount += 1
            End If

            If index > 0 And index < lastCells.Count Then
                cell.color = lastColors.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            End If
            If usedColors.Contains(cell.color) Then cell.color = randomCellColor()
            usedColors.Add(cell.color)

            If dst2.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = redCells.Count + 1
                redCells.Add(cell)
                dst2(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)

                setTrueText(CStr(cell.index), cell.maxDist, 2)
                setTrueText(CStr(cell.index), cell.maxDist, 3)
            End If
        Next

        labels(3) = "There were " + CStr(redCells.Count) + " collected cells and " + CStr(motionCount) +
                    " cells removed because of motion.  "

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






Public Class RedMin_ContourVsFeatureLess : Inherits VB_Algorithm
    Dim redCore As New RedCloud_CPP
    Dim contour As New Contour_WholeImage
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "Contour_WholeImage Input", "RedCloud_CPP - toggling between Contour and Featureless inputs",
                  "FeatureLess_Basics Input"}
        desc = "Compare Contour_WholeImage and FeatureLess_Basics as input to RedCloud_CPP"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static useContours = findRadio("Use Contour_WholeImage")

        contour.Run(src)
        dst1 = contour.dst2

        fLess.Run(src)
        dst3 = fLess.dst2

        If task.toggleOn Then redCore.Run(dst3) Else redCore.Run(dst1)
        dst2 = redCore.dst3
    End Sub
End Class








Public Class RedMin_Blobs : Inherits VB_Algorithm
    Dim rMin As New RedColor_Basics
    Public Sub New()
        gOptions.DebugSlider.Value = 0
        desc = "Select blobs by size using the DebugSlider in the global options"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        Dim index = gOptions.DebugSlider.Value
        If index < rMin.redCells.Count Then
            dst3.SetTo(0)
            Dim cell = rMin.redCells(index)
            dst3(cell.rect).SetTo(cell.color, cell.mask)
        End If
    End Sub
End Class








Public Class RedMin_RedCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim rMin As New RedColor_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.UseDepth.Checked = True
        desc = "Use the RedMin output to combine the RedCloud output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst1 = rMin.dst3
        labels(1) = rMin.labels(2)

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        dst3.SetTo(0)
        For Each rc In redC.redCells
            Dim color = dst1.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            dst3(rc.rect).SetTo(color, rc.mask)
        Next
    End Sub
End Class






Public Class RedMin_Gaps : Inherits VB_Algorithm
    Dim rMin As New RedColor_Basics
    Dim frames As New History_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        advice = ""
        desc = "Find the gaps that are different in the RedColor_Basics results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        frames.Run(rMin.dst2.InRange(0, 0))
        dst3 = frames.dst2

        If task.cellSelect.index <> 0 Then dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)

        Dim count = dst3.CountNonZero
        labels(3) = "Unclassified pixel count = " + CStr(count) + " or " + Format(count / src.Total, "0%")
    End Sub
End Class









Public Class RedMin_Motion : Inherits VB_Algorithm
    Public motion As New Motion_Basics
    Public redCells As New List(Of rcData)
    Public sortedCells As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 25
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use absDiff to build a mask of cells that changed."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motion.Run(src)
        dst3 = motion.dst2

        If standalone Then
            Static redCore As New RedCloud_CPP
            redCore.Run(src)
            dst2 = redCore.dst3
            labels(2) = redCore.labels(3)
            sortedCells = redCore.sortedCells
        End If

        redCells.Clear()
        For Each key In sortedCells
            Dim cell = key.Value
            Dim tmp As cv.Mat = cell.mask And motion.dst2(cell.rect)
            If tmp.CountNonZero Then cell.motionFlag = True
            redCells.Add(cell)
        Next
    End Sub
End Class






Public Class RedMin_FindPixels_CPP : Inherits VB_Algorithm
    Public Sub New()
        cPtr = RedMin_FindPixels_Open()
        advice = ""
        desc = "Create the list of pixels in a RedMin Cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.drawRect <> New cv.Rect Then src = src(task.drawRect)
        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim classCount = RedMin_FindPixels_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If classCount = 0 Then Exit Sub
        Dim pixelData = New cv.Mat(classCount, 1, cv.MatType.CV_8UC3, RedMin_FindPixels_Pixels(cPtr))
        setTrueText(CStr(classCount) + " unique BGR pixels were found in the src." + vbCrLf +
                    "Or " + Format(classCount / src.Total, "0%") + " of the input.")
    End Sub
    Public Sub Close()
        RedMin_FindPixels_Close(cPtr)
    End Sub
End Class






Public Class RedMin_PixelClassifier : Inherits VB_Algorithm
    Dim pixel As New Hist3D_Pixel
    Dim rMin As New RedColor_Basics
    Public Sub New()
        advice = ""
        desc = "Speed up RedColor_Basics by using the backprojection of the 3D color histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        pixel.Run(src)

        rMin.Run(pixel.dst2)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        If task.cellSelect.index <> 0 Then dst2(task.cellSelect.rect).SetTo(cv.Scalar.White, task.cellSelect.mask)
    End Sub
End Class





Public Class RedMin_PixelVector3D : Inherits VB_Algorithm
    Dim rMin As New RedColor_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Public pixelVector As New List(Of List(Of Single))
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.HistBinSlider.Value = 3
        labels = {"", "RedColor_Basics output", "3D Histogram counts for each of the cells at left", ""}
        desc = "Identify RedMin cells and create a vector for each cell's 3D histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        Dim maxRegion = 20

        Static distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDouble)
        If heartBeat() Then
            pixelVector.Clear()
            strOut = "3D histogram counts for each cell - " + CStr(maxRegion) + " largest only for readability..." + vbCrLf
            For Each cell In rMin.redCells
                hColor.inputMask = cell.mask
                hColor.Run(src(cell.rect))
                pixelVector.Add(hColor.histArray.ToList)
                strOut += "(" + CStr(cell.index) + ") "
                For Each count In hColor.histArray
                    strOut += CStr(count) + ","
                Next
                strOut += vbCrLf
                If cell.index >= maxRegion Then Exit For
            Next
        End If
        setTrueText(strOut, 3)

        dst1.SetTo(0)
        dst2.SetTo(0)
        For Each cell In rMin.redCells
            task.color(cell.rect).CopyTo(dst2(cell.rect), cell.mask)
            dst1(cell.rect).SetTo(cell.color, cell.mask)
            If cell.index <= maxRegion Then setTrueText(CStr(cell.index), cell.maxDist, 2)
        Next
        labels(2) = rMin.labels(3)
    End Sub
End Class





Public Class RedMin_PixelVectors : Inherits VB_Algorithm
    Public rMin As New RedColor_Basics
    Dim hVector As New Hist3Dcolor_Vector
    Public pixelVector As New List(Of Single())
    Public redCells As New List(Of rcData)
    Public Sub New()
        labels = {"", "", "RedColor_Basics output", ""}
        desc = "Create a vector for each cell's 3D histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        Static distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDouble)
        pixelVector.Clear()
        For Each cell In rMin.redCells
            hVector.inputMask = cell.mask
            hVector.Run(src(cell.rect))
            pixelVector.Add(hVector.histArray)
        Next
        redCells = rMin.redCells

        setTrueText("3D color histograms were created for " + CStr(pixelVector.Count) + " cells", 3)
    End Sub
End Class