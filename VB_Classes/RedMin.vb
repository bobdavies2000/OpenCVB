Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class RedMin_Basics : Inherits VB_Algorithm
    Public minCore As New RedMin_Core
    Public minCells As New List(Of segCell)
    Dim lastColors As cv.Mat
    Dim lastMap As cv.Mat = dst2.Clone
    Public Sub New()
        redOptions.DesiredCellSlider.Value = 30
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "Mask of active RedMin cells", "CV_8U representation of minCells", ""}
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        minCore.Run(src)
        Dim lastCells As New List(Of segCell)(minCells)
        If firstPass Then lastColors = dst3.Clone

        minCells.Clear()
        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Vec3b)({black})
        For Each key In minCore.sortedCells
            Dim cell = key.Value
            Dim index = lastMap.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X)

            If index > 0 And index < lastCells.Count Then
                cell.color = lastColors.Get(Of cv.Vec3b)(cell.maxDist.Y, cell.maxDist.X)
            End If
            If usedColors.Contains(cell.color) Then cell.color = randomCellColor()
            usedColors.Add(cell.color)

            If dst2.Get(Of Byte)(cell.maxDist.Y, cell.maxDist.X) = 0 Then
                cell.index = minCells.Count + 1
                minCells.Add(cell)
                dst2(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)

                setTrueText(CStr(cell.index), cell.maxDist, 2)
                setTrueText(CStr(cell.index), cell.maxDist, 3)
            End If
        Next

        labels(3) = CStr(minCells.Count) + " cells were identified."

        task.cellSelect = New segCell
        If task.clickPoint = New cv.Point(0, 0) Then
            If minCells.Count > 2 Then
                task.clickPoint = minCells(0).maxDist
                task.cellSelect = minCells(0)
            End If
        Else
            Dim index = dst2.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            If index <> 0 Then task.cellSelect = minCells(index - 1)
        End If
        lastColors = dst3.Clone
        lastMap = dst2.Clone
        If minCells.Count > 0 Then dst1 = vbPalette(lastMap * 255 / minCells.Count)
    End Sub
End Class





Public Class RedMin_BasicsMotion : Inherits VB_Algorithm
    Public minCore As New RedMin_Core
    Public minCells As New List(Of segCell)
    Public rMotion As New RedMin_Motion
    Dim lastColors = dst3.Clone
    Dim lastMap As cv.Mat = dst2.Clone
    Public Sub New()
        redOptions.DesiredCellSlider.Value = 30
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "Mask of active RedMin cells", "CV_8U representation of minCells", ""}
        desc = "Track the color cells from floodfill - trying a minimalist approach to build cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        minCore.Run(src)

        rMotion.sortedCells = minCore.sortedCells
        rMotion.Run(task.color.Clone)

        Dim lastCells As New List(Of segCell)(minCells)

        minCells.Clear()
        dst2.SetTo(0)
        dst3.SetTo(0)
        Dim usedColors = New List(Of cv.Vec3b)({black})
        Dim motionCount As Integer
        For Each cell In rMotion.minCells
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
                cell.index = minCells.Count + 1
                minCells.Add(cell)
                dst2(cell.rect).SetTo(cell.index, cell.mask)
                dst3(cell.rect).SetTo(cell.color, cell.mask)

                setTrueText(CStr(cell.index), cell.maxDist, 2)
                setTrueText(CStr(cell.index), cell.maxDist, 3)
            End If
        Next

        labels(3) = "There were " + CStr(minCells.Count) + " collected cells and " + CStr(motionCount) +
                    " cells removed because of motion.  "

        task.cellSelect = New segCell
        If task.clickPoint = New cv.Point(0, 0) Then
            If minCells.Count > 2 Then
                task.clickPoint = minCells(0).maxDist
                task.cellSelect = minCells(0)
            End If
        Else
            Dim index = dst2.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
            If index <> 0 Then task.cellSelect = minCells(index - 1)
        End If
        lastColors = dst3.Clone
        lastMap = dst2.Clone
        If minCells.Count > 0 Then dst1 = vbPalette(lastMap * 255 / minCells.Count)
    End Sub
End Class





Public Class RedMin_Core : Inherits VB_Algorithm
    Public sortedCells As New SortedList(Of Integer, segCell)(New compareAllowIdenticalIntegerInverted)
    Public inputMask As cv.Mat
    Dim fLess As New FeatureLess_BasicsAccum
    Public Sub New()
        cPtr = FloodCell_Open()
        desc = "Another minimalist approach to building RedCloud color-based cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 1 Then
            fLess.Run(src)
            src = fLess.dst2
        End If

        Dim imagePtr As IntPtr
        If inputMask Is Nothing Then
            Dim inputData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            imagePtr = FloodCell_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols,
                                     src.Type, redOptions.imageThresholdPercent, redOptions.DesiredCellSlider.Value, 0)
            handleInput.Free()
        Else
            Dim inputData(src.Total - 1) As Byte
            Marshal.Copy(src.Data, inputData, 0, inputData.Length)
            Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

            Dim maskData(inputMask.Total - 1) As Byte
            Marshal.Copy(inputMask.Data, maskData, 0, maskData.Length)
            Dim handleMask = GCHandle.Alloc(maskData, GCHandleType.Pinned)

            imagePtr = FloodCell_Run(cPtr, handleInput.AddrOfPinnedObject(), handleMask.AddrOfPinnedObject(), src.Rows, src.Cols,
                                     src.Type, redOptions.imageThresholdPercent, redOptions.DesiredCellSlider.Value, 0)
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
            Dim cell As New segCell
            cell.index = i + 1
            cell.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            cell.mask = dst2(cell.rect).InRange(cell.index, cell.index)
            Dim contour = contourBuild(cell.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(cell.mask, contour, 255, -1)

            cell.pixels = sizeData.Get(Of Integer)(i, 0)
            cell.floodPoint = floodPointData.Get(Of cv.Point)(i, 0)
            cell.mask.Rectangle(New cv.Rect(0, 0, cell.mask.Width, cell.mask.Height), 0, 1)
            Dim pt = vbGetMaxDist(cell.mask)
            cell.maxDist = New cv.Point(pt.X + cell.rect.X, pt.Y + cell.rect.Y)
            sortedCells.Add(cell.pixels, cell)
        Next
        If heartBeat() Then labels(2) = "CV_8U format - " + CStr(classCount) + " cells were identified."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = FloodCell_Close(cPtr)
    End Sub
End Class





Public Class RedMin_BasicsAssist : Inherits VB_Algorithm
    Public rMin As New RedMin_BasicsMotion
    Dim mats As New Mat_4Click
    Public Sub New()
        desc = "Debug assistant for RedMin_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        mats.mat(0) = rMin.dst3
        mats.mat(1) = rMin.minCore.dst3
        mats.mat(2) = rMin.rMotion.dst3
        mats.mat(3) = rMin.rMotion.motion.dst3

        mats.Run(empty)
        dst2 = mats.dst2
        dst3 = mats.dst3
        labels(3) = rMin.labels(3)
    End Sub
End Class






Public Class RedMin_ContourVsFeatureLess : Inherits VB_Algorithm
    Dim redMin As New RedMin_Core
    Dim contour As New Contour_WholeImage
    Dim fLess As New FeatureLess_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "Contour_WholeImage Input", "Redmin_Core - toggling between Contour and Featureless inputs",
                  "FeatureLess_Basics Input"}
        desc = "Compare Contour_WholeImage and FeatureLess_Basics as input to RedMin_Core"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static useContours = findRadio("Use Contour_WholeImage")

        contour.Run(src)
        dst1 = contour.dst2

        fLess.Run(src)
        dst3 = fLess.dst2

        If task.toggleOn Then redMin.Run(dst3) Else redMin.Run(dst1)
        dst2 = redMin.dst3
    End Sub
End Class








Public Class RedMin_Blobs : Inherits VB_Algorithm
    Dim rMin As New RedMin_Basics
    Public Sub New()
        gOptions.DebugSlider.Value = 0
        desc = "Select blobs by size using the DebugSlider in the global options"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        Dim index = gOptions.DebugSlider.Value
        If index < rMin.minCells.Count Then
            dst3.SetTo(0)
            Dim cell = rMin.minCells(index)
            dst3(cell.rect).SetTo(cell.color, cell.mask)
        End If
    End Sub
End Class








Public Class RedMin_RedCloud : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim rMin As New RedMin_Basics
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
    Dim rMin As New RedMin_Basics
    Dim frames As New History_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        advice = ""
        desc = "Find the gaps that are different in the RedMin_Basics results."
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
    Public minCells As New List(Of segCell)
    Public sortedCells As New SortedList(Of Integer, segCell)(New compareAllowIdenticalIntegerInverted)
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 25
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Use absDiff to build a mask of cells that changed."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        motion.Run(src)
        dst3 = motion.dst2

        If standalone Then
            Static minCore As New RedMin_Core
            minCore.Run(src)
            dst2 = minCore.dst3
            labels(2) = minCore.labels(3)
            sortedCells = minCore.sortedCells
        End If

        Dim minPixels = gOptions.minPixelsSlider.Value
        If task.quarterBeat Then dst3.SetTo(0)

        minCells.Clear()
        For Each key In sortedCells
            Dim cell = key.Value
            Dim tmp As cv.Mat = cell.mask And motion.dst3(cell.rect)
            If tmp.CountNonZero Then cell.motionFlag = True
            minCells.Add(cell)
        Next
    End Sub
End Class






Public Class RedMin_FindPixels_CPP : Inherits VB_Algorithm
    Public Sub New()
        task.drawRect = New cv.Rect(100, 100, 80, 60)
        cPtr = RedMin_FindPixels_Open()
        advice = ""
        desc = "Create the list of pixels in a RedMin Cell"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        ' src = src(task.drawRect)
        Dim cppData(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, cppData, 0, cppData.Length - 1)
        Dim handleSrc = GCHandle.Alloc(cppData, GCHandleType.Pinned)
        Dim classCount = RedMin_FindPixels_RunCPP(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols)
        handleSrc.Free()

        If classCount = 0 Then Exit Sub
        Dim pixelData = New cv.Mat(classCount, 1, cv.MatType.CV_8UC3, FloodCell_Sizes(cPtr))
        setTrueText(CStr(classCount) + " unique BGR pixels were found in the src." + vbCrLf +
                    "Or " + Format(classCount / src.Total, "0%") + " of the input.")
    End Sub
    Public Sub Close()
        RedMin_FindPixels_Close(cPtr)
    End Sub
End Class






Public Class RedMin_PixelClassifier : Inherits VB_Algorithm
    Dim pixel As New Hist3D_Pixel
    Dim rMin As New RedMin_Basics
    Public Sub New()
        advice = ""
        desc = "Speed up RedMin_Basics by using the backprojection of the 3D color histogram."
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
    Dim rMin As New RedMin_Basics
    Dim hColor As New Hist3Dcolor_Basics
    Public pixelVector As New List(Of List(Of Single))
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.HistBinSlider.Value = 3
        labels = {"", "RedMin_Basics output", "3D Histogram counts for each of the cells at left", ""}
        desc = "Identify RedMin cells and create a vector for each cell's 3D histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        Dim maxRegion = 20

        Static distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDouble)
        If heartBeat() Then
            pixelVector.Clear()
            strOut = "3D histogram counts for each cell - " + CStr(maxRegion) + " largest only for readability..." + vbCrLf
            For Each cell In rMin.minCells
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
        For Each cell In rMin.minCells
            task.color(cell.rect).CopyTo(dst2(cell.rect), cell.mask)
            dst1(cell.rect).SetTo(cell.color, cell.mask)
            If cell.index <= maxRegion Then setTrueText(CStr(cell.index), cell.maxDist, 2)
        Next
        labels(2) = rMin.labels(3)
    End Sub
End Class





Public Class RedMin_PixelVectors : Inherits VB_Algorithm
    Public rMin As New RedMin_Basics
    Dim hVector As New Hist3Dcolor_Vector
    Public pixelVector As New List(Of Single())
    Public minCells As New List(Of segCell)
    Public Sub New()
        labels = {"", "", "RedMin_Basics output", ""}
        desc = "Create a vector for each cell's 3D histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(3)

        Static distances As New SortedList(Of Double, Integer)(New compareAllowIdenticalDouble)
        pixelVector.Clear()
        For Each cell In rMin.minCells
            hVector.inputMask = cell.mask
            hVector.Run(src(cell.rect))
            pixelVector.Add(hVector.histArray)
        Next
        minCells = rMin.minCells

        setTrueText("3D color histograms were created for " + CStr(pixelVector.Count) + " cells", 3)
    End Sub
End Class