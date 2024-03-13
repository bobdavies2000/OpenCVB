Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Flood_Basics : Inherits VB_Algorithm
    Public redCells As New List(Of rcDataNew)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Dim binar4 As New Binarize_Split4
    Dim genCells As New RedCloud_GenCells
    Dim redCPP As New RedCloud_MaskNone
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

        setSelectedContour(redCells, cellMap)
        identifyCells(redCells)

        Dim cellCount = Math.Min(redOptions.identifyCount, redCells.Count)
        If task.heartBeat Then labels(2) = $"{redCells.Count} cells identified and the largest {cellCount} are numbered below."
    End Sub
End Class







Public Class Flood_BasicsMask : Inherits VB_Algorithm
    Public redCells As New List(Of rcDataNew)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public binarizedImage As cv.Mat
    Public inputMask As cv.Mat
    Dim genCells As New RedCloud_GenCells
    Dim redCPP As New RedCloud_Mask
    Public buildInputMask As Boolean
    Public Sub New()
        desc = "Floodfill by color as usual but this is run repeatedly with the different tiers."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Or buildInputMask Then
            Static floodInput As New Flood_ColorAndTiers
            floodInput.Run(src)
            inputMask = floodInput.dst2
            src = floodInput.colorC.dst2
            dst3 = floodInput.dst3
            labels = floodInput.labels
        End If

        redCPP.inputmask = inputMask
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
        If task.heartBeat Then labels(2) = $"{redCells.Count} cells identified and the largest {cellCount} are numbered below."
        setSelectedContour(redCells, cellMap)
        identifyCells(redCells)
    End Sub
End Class






Public Class Flood_Point : Inherits VB_Algorithm
    Public pixelCount As Integer
    Public rect As cv.Rect
    Dim edges As New Edge_BinarizedSobel
    Public centroid As cv.Point2f
    Public initialMask As New cv.Mat
    Public pt As cv.Point ' this is the floodfill point
    Dim options As New Options_Flood
    Public Sub New()
        labels(2) = "Input image to floodfill"
        labels(3) = If(standaloneTest(), "Flood_Point standaloneTest() just shows the edges", "Resulting mask from floodfill")
        desc = "Use floodfill at a single location in a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src.Clone()
        If standaloneTest() Then
            pt = New cv.Point(msRNG.Next(0, dst2.Width - 1), msRNG.Next(0, dst2.Height - 1))
            edges.Run(src)
            dst2 = edges.mats.dst2
            dst3 = edges.mats.mat(edges.mats.quadrant)
        Else
            Dim maskPlus = New cv.Mat(New cv.Size(src.Width + 2, src.Height + 2), cv.MatType.CV_8UC1, 0)
            Dim maskRect = New cv.Rect(1, 1, dst2.Width, dst2.Height)

            Dim zero = New cv.Scalar(0)
            pixelCount = cv.Cv2.FloodFill(dst2, maskPlus, New cv.Point(CInt(pt.X), CInt(pt.Y)), cv.Scalar.White, rect, zero, zero, options.floodFlag Or (255 << 8))
            dst3 = maskPlus(maskRect).Clone
            pixelCount = pixelCount
            Dim m = cv.Cv2.Moments(maskPlus(rect), True)
            centroid = New cv.Point2f(rect.X + m.M10 / m.M00, rect.Y + m.M01 / m.M00)
            labels(3) = CStr(pixelCount) + " pixels at point pt(x=" + CStr(pt.X) + ",y=" + CStr(pt.Y)
        End If
    End Sub
End Class










Public Class Flood_Click : Inherits VB_Algorithm
    Dim edges As New Edge_BinarizedSobel
    Dim flood As New Flood_Point
    Public Sub New()
        flood.pt = New cv.Point(msRNG.Next(0, dst2.Width - 1), msRNG.Next(0, dst2.Height - 1))
        labels = {"", "", "Click anywhere TypeOf floodfill that area.", "Edges for 4 different light levels."}
        desc = "FloodFill where the mouse clicks"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.mouseClickFlag Then
            flood.pt = task.clickPoint
            task.mouseClickFlag = False ' preempt any other uses
        End If

        edges.Run(src)
        dst2 = edges.dst3

        If flood.pt.X Or flood.pt.Y Then
            flood.Run(dst2.Clone)
            dst2.CopyTo(dst3)
            If flood.pixelCount > 0 Then dst3.SetTo(255, flood.dst3)
        End If
    End Sub
End Class









Public Class Flood_TopX : Inherits VB_Algorithm
    Dim flood As New Flood_PointList
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Show the X largest regions in FloodFill", 1, 100, 50)
        desc = "Get the top X size regions in the floodfill output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static topXslider = findSlider("Show the X largest regions in FloodFill")
        Static desiredRegions As Integer
        If desiredRegions <> topXslider.Value Then
            flood.pointList.Clear()
            desiredRegions = topXslider.Value
        End If

        flood.Run(src)
        dst3 = flood.dst2

        Dim rc As New rcDataOld
        If task.motionFlag Or task.optionsChanged Then dst2.SetTo(0)
        Dim rcCount = Math.Min(flood.redCells.Count, topXslider.Value)

        For i = 1 To rcCount - 1
            rc = flood.redCells(i)
            Dim c = src.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
            c = New cv.Vec3b(If(c(0) < 50, 50, c(0)), If(c(1) < 50, 50, c(1)), If(c(2) < 50, 50, c(2)))
            dst2(rc.rect).SetTo(c, rc.mask)
        Next
        labels(2) = CStr(rcCount) + " regions were found.  Use the nearby slider to increase."
    End Sub
End Class










Public Class Flood_PointList : Inherits VB_Algorithm
    Public pointList As New List(Of cv.Point)
    Public redCells As New List(Of rcDataOld)
    Public cellMap As cv.Mat
    Public options As New Options_Flood
    Dim reduction As New Reduction_Basics
    Public Sub New()
        cellMap = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "", "Grid Dots are the input points."}
        desc = "The bare minimum to use floodfill - supply points, get mask and rect"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst3 = src.Clone

        reduction.Run(src)
        dst0 = reduction.dst2.Clone

        If task.optionsChanged Or standaloneTest() Then
            For y = options.stepSize To dst3.Height - 1 Step options.stepSize
                For x = options.stepSize To dst3.Width - options.stepSize - 1 Step options.stepSize
                    Dim p1 = New cv.Point(x, y)
                    pointList.Add(p1)
                    dst3.Circle(p1, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
                Next
            Next

            If task.optionsChanged Then
                redCells.Clear()
                cellMap.SetTo(0)
                dst2.SetTo(0)
            End If
        End If

        Dim rect As New cv.Rect

        Dim totalPixels As Integer
        Dim SizeSorted As New SortedList(Of Integer, rcDataOld)(New compareAllowIdenticalIntegerInverted)
        Dim maskPlus = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8UC1, 0)
        Dim index = 1
        For Each pt In pointList
            Dim floodFlag = cv.FloodFillFlags.FixedRange Or (index << 8)
            Dim rc = New rcDataOld
            rc.pixels = cv.Cv2.FloodFill(dst0, maskPlus, pt, 255, rc.rect, 0, 0, floodFlag)
            If rc.pixels >= options.minPixels And rc.rect.Width < dst2.Width And rc.rect.Height < dst2.Height And
               rc.rect.Width > 0 And rc.rect.Height > 0 Then
                rc.mask = maskPlus(rc.rect).InRange(index, index)

                rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone)
                rc.maxDist = vbGetMaxDist(rc)

                totalPixels += rc.pixels
                SizeSorted.Add(rc.pixels, rc)
            End If
        Next
        labels(2) = CStr(SizeSorted.Count) + " regions were found with " + Format(totalPixels / dst0.Total, "0%") + " pixels flooded"

        Dim lastcellMap = cellMap.Clone
        Dim lastCells = New List(Of rcDataOld)(redCells)
        cellMap.SetTo(0)
        redCells.Clear()
        redCells.Add(New rcDataOld) ' stay away from 0...

        dst2.SetTo(0)
        Dim usedColors As New List(Of cv.Vec3b)({black})
        For i = 0 To SizeSorted.Count - 1
            Dim rc = SizeSorted.ElementAt(i).Value
            rc.index = redCells.Count
            Dim lrc = If(lastCells.Count > 0, lastCells(lastcellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)), New rcDataOld)
            rc.indexLast = lrc.index
            rc.color = lrc.color
            If usedColors.Contains(rc.color) Then rc.color = randomCellColor()

            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
            vbDrawContour(cellMap(rc.rect), rc.contour, rc.index, -1)

            redCells.Add(rc)
            usedColors.Add(rc.color)
        Next
    End Sub
End Class








Public Class Flood_FeaturelessHulls : Inherits VB_Algorithm
    Public classCount As Integer
    Dim redC As New RedCloud_Tight
    Dim redCells As New List(Of rcDataOld)
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels = {"", "", "", "Palette output of image at left"}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "FloodFill the input and paint it with LUT"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standaloneTest() Then
            Static fless As New FeatureLess_Basics
            fless.Run(src)
            src = fless.dst2
        End If

        redC.Run(src)
        classCount = redC.redCells.Count

        dst2.SetTo(0)
        redCells.Clear()
        For Each rp In redC.redCells
            Dim contour = contourBuild(rp.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            Dim hull = cv.Cv2.ConvexHull(contour, True).ToList
            vbDrawContour(dst2(rp.rect), hull, rp.index, -1)
            redCells.Add(rp)
        Next

        labels(2) = "Hulls were added for each of the " + CStr(redCells.Count) + " cells identified"
        dst3 = vbPalette(dst2 * 255 / redCells.Count)
    End Sub
End Class







'Public Class Flood_Tiers : Inherits VB_Algorithm
'    Dim flood As New Flood_Basics
'    Dim tiers As New Contour_DepthTiers
'    Dim plot As New Plot_Histogram
'    Public redCells As New List(Of rcDataOld)
'    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
'    Public Sub New()
'        plot.removeZeroEntry = False
'        desc = "Subdivide the Flood_Basics cells using depth tiers."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        flood.Run(src)
'        dst2 = flood.dst2

'        tiers.Run(src)

'        redCells.Clear()
'        cellMap.SetTo(0)
'        Dim ranges = {New cv.Rangef(-1, tiers.classCount + 1)}
'        For Each rc In flood.redCells
'            rc.depthMask = rc.mask And task.depthMask(rc.rect)
'            If rc.depthMask.CountNonZero Then
'                cv.Cv2.CalcHist({tiers.dst2(rc.rect)}, {0}, New cv.Mat, rc.tierHist, 1, {tiers.classCount}, ranges)

'                ReDim rc.tierHistArray(tiers.classCount - 1)
'                Dim samples(rc.tierHist.Total - 1) As Single
'                Marshal.Copy(rc.tierHist.Data, rc.tierHistArray, 0, rc.tierHistArray.Length)
'            End If
'            cellMap(rc.rect).SetTo(rc.index, rc.mask)
'            redCells.Add(rc)
'        Next

'        setSelectedContour(redCells, cellMap)

'        If task.rc.tierHist.Rows > 0 Then
'            plot.Run(task.rc.tierHist)
'            dst3 = plot.dst3
'        End If
'        identifyCells(redCells)
'    End Sub
'End Class








Public Class Flood_Stats : Inherits VB_Algorithm
    Dim flood As New Flood_ByColorWithinDepth
    Dim stats As New Cell_Basics
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







Public Class Flood_ByColorWithinDepth : Inherits VB_Algorithm
    Dim tiers As New Contour_DepthTiers
    Dim floodMask As New Flood_BasicsMask
    Dim binar4 As New Binarize_Split4
    Public redCells As New List(Of rcDataNew)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        desc = "Flood the color image by tiers"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binar4.Run(src)

        tiers.Run(src)

        floodMask.binarizedImage = binar4.dst3
        Dim sortedCells As New SortedList(Of Integer, rcDataNew)(New compareAllowIdenticalIntegerInverted)
        For i = 1 To tiers.contourlist.Count - 1
            dst1 = tiers.dst2.InRange(i, i)
            floodMask.inputMask = Not dst1
            dst0.SetTo(0)
            binar4.dst2.CopyTo(dst0, dst1)
            floodMask.Run(dst0)
            For Each rc In floodMask.redCells
                sortedCells.Add(rc.pixels, rc)
            Next
        Next

        dst2.SetTo(0)
        cellMap.SetTo(0)
        redCells.Clear()
        redCells.Add(New rcDataNew)
        For Each rc In sortedCells.Values
            Dim val = cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If val = 0 Then
                rc.index = redCells.Count
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                cellMap(rc.rect).SetTo(rc.index, rc.mask)
                redCells.Add(rc)
                If redCells.Count >= 250 Then Exit For
            End If
        Next
        dst3 = vbPalette(cellMap)
        labels(2) = $"{redCells.Count} cells were identified."
        setSelectedContour(redCells, cellMap)
    End Sub
End Class









Public Class Flood_Cell : Inherits VB_Algorithm
    Dim tiers As New Contour_DepthTiers
    Dim floodMask As New Flood_BasicsMask
    Dim redCells As New List(Of rcDataNew)
    Dim cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        redOptions.DesiredCellSlider.Value = 250
        gOptions.useFilter.Checked = False
        desc = "Run RedCloud on an individual cell with limited count of desired cells."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim ttOffset = tiers.options.trueTextOffset
        If standalone Then
            Static flood As New Flood_Basics  ' <<<<<< switch to Flood_ByColorWithinDepth to see the difference.
            flood.Run(src)
            dst2 = flood.dst2
            labels = flood.labels
        End If

        tiers.Run(src)
        dst1 = New cv.Mat(task.rc.mask.Size, cv.MatType.CV_8U, 0)
        tiers.dst2(task.rc.rect).CopyTo(dst1, task.rc.mask)

        floodMask.inputMask = Not task.rc.mask
        floodMask.binarizedImage = dst1
        floodMask.Run(dst1)

        redCells.Clear()
        cellMap.SetTo(0)
        Dim offset As Integer = ttOffset
        If task.rc.maxDist.X > dst2.Width / 2 Then offset *= -1
        For Each rc In floodMask.redCells
            If rc.pixels >= tiers.options.minPixels Then
                rc.rect.X += task.rc.rect.X
                rc.rect.Y += task.rc.rect.Y
                rc.floodPoint.X += task.rc.rect.X
                rc.floodPoint.Y += task.rc.rect.Y
                rc.maxDist.X += task.rc.rect.X
                rc.maxDist.Y += task.rc.rect.Y
                redCells.Add(rc)
                cellMap(rc.rect).SetTo(rc.index, rc.mask)
                If rc.index <= redOptions.identifyCount And rc.index > 0 Then
                    Dim pt = New cv.Point(rc.floodPoint.X + offset, rc.floodPoint.Y)
                    setTrueText(Format(rc.depthMean(2), fmt1), pt, 3)
                End If
            End If
        Next

        dst3.SetTo(0)
        dst3(task.rc.rect) = vbPalette(cellMap(task.rc.rect) * 255 / redCells.Count)
        vbDrawContour(dst3(task.rc.rect), task.rc.contour, cv.Scalar.White)

        labels(3) = $"{redCells.Count} {If(redCells.Count = 1, "cell was ", "cells were ")} identified within the selected cell."
        If standalone Then identifyCells(floodMask.redCells)
    End Sub
End Class







Public Class Flood_ColorAndTiers : Inherits VB_Algorithm
    Public tiers As New Depth_TiersZ
    Public colorC As New Color_Basics
    Public Sub New()
        redOptions.ColorSource.SelectedItem() = "Binarize_Split4"
        desc = "Build the tiers output and the color source output for use with other flood algorithms."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        tiers.Run(src)
        Dim tier = gOptions.DebugSlider.Value
        If tier >= tiers.classCount Then tier = 0

        If tier = 0 Then
            dst2 = Not tiers.dst2.InRange(0, 1)
        Else
            dst2 = Not tiers.dst2.InRange(tier, tier)
        End If

        labels(2) = tiers.labels(2)

        colorC.Run(src)
        dst3 = colorC.dst3

        labels(2) = "Depth_TiersZ output for tier " + CStr(gOptions.DebugSlider.Value)
        labels(3) = "Color source selected = " + redOptions.colorInputName
    End Sub
End Class







Public Class Flood_NeighborContains : Inherits VB_Algorithm
    Dim flood As New Flood_BasicsMask
    Public redCells As New List(Of rcDataNew)
    Public Sub New()
        flood.buildInputMask = True
        desc = "Attach smaller cells to the largest X cells or classify them as other."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            flood.Run(src)
            dst2 = flood.dst2
            redCells = flood.redCells
            labels = flood.labels
        End If

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
            If nabs.Count > 0 Then rc.nab = nabs.Min()
            If contains.Count > 0 Then rc.container = contains.Min()
            redCells(i) = rc
        Next

        identifyCells(redCells)
    End Sub
End Class
