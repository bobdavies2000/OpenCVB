Imports System.Runtime.InteropServices
Imports cv = OpenCvSharp
Public Class Flood_Basics : Inherits VB_Algorithm
    Public classCount As Integer
    Dim palette As New Palette_MyColorMap
    Public redCells As New List(Of rcData)
    Public cellMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
    Public Sub New()
        palette.colorMap = New cv.Mat(256, 1, cv.MatType.CV_8UC3, task.vecColors)
        redOptions.ColorSource.SelectedText = "Binarize_Split4"
        cPtr = RedCloud_Open()
        redOptions.DesiredCellSlider.Value = 50
        vbAddAdvice(traceName + ": redOptions 'Desired RedCloud Cells' determines how many regions will be found.")
        labels(3) = "FloodFill regions displayed with a custom palette (task.vecColors)"
        desc = "Simple Floodfill each region but prepare the mask, rect, floodpoints, and pixel counts."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        src = task.color.Clone
        If src.Channels <> 1 Then
            Static colorClass As New Color_Basics
            colorClass.Run(src)
            src = colorClass.dst2
        End If

        Dim inputData(src.Total - 1) As Byte
        Marshal.Copy(src.Data, inputData, 0, inputData.Length)
        Dim handleInput = GCHandle.Alloc(inputData, GCHandleType.Pinned)

        Dim imagePtr = RedCloud_Run(cPtr, handleInput.AddrOfPinnedObject(), 0, src.Rows, src.Cols, src.Type,
                                    redOptions.DesiredCellSlider.Value, 0, 1, 0)
        handleInput.Free()

        classCount = RedCloud_Count(cPtr)
        If classCount = 0 Then Exit Sub ' no data to process.
        dst1 = New cv.Mat(src.Rows, src.Cols, cv.MatType.CV_8U, imagePtr).Clone

        Dim rectData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC4, RedCloud_Rects(cPtr))
        Dim floodPointData = New cv.Mat(classCount, 1, cv.MatType.CV_32SC2, RedCloud_FloodPoints(cPtr))
        redCells.Clear()
        redCells.Add(New rcData)
        cellMap.SetTo(0)
        For i = 0 To classCount - 1
            Dim rc As New rcData
            rc.index = redCells.Count
            rc.rect = validateRect(rectData.Get(Of cv.Rect)(i, 0))
            rc.mask = dst1(rc.rect).InRange(rc.index, rc.index).Threshold(0, 255, cv.ThresholdTypes.Binary)

            rc.contour = contourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            vbDrawContour(rc.mask, rc.contour, 255, -1)

            rc.floodPoint = floodPointData.Get(Of cv.Point)(i, 0)

            rc.maxDist = vbGetMaxDist(rc)

            Dim index = cellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
            If index = 0 Then
                cellMap(rc.rect).SetTo(rc.index, rc.mask)
                rc.color = dst3.Get(Of cv.Vec3b)(rc.maxDist.Y, rc.maxDist.X)
                If rc.color = black Then rc.color = palette.colorMap.Get(Of cv.Vec3b)(rc.index, 0)

                rc.pixels = rc.mask.CountNonZero
                redCells.Add(rc)
            Else
                Dim k = 0
                '    cellMap(rc.rect).SetTo(redCells(index).index, rc.mask)
                '    dst1(rc.rect).SetTo(index, rc.mask)

                '    Dim r = redCells(index).rect.Union(rc.rect)
                '    redCells(index).mask = dst1(r).InRange(index, index).Threshold(0, 255, cv.ThresholdTypes.Binary)
                '    redCells(index).contour = contourBuild(redCells(index).mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
                '    vbDrawContour(redCells(index).mask, redCells(index).contour, 255, -1)

                '    redCells(index).pixels = redCells(index).mask.CountNonZero
            End If
        Next

        palette.Run(cellMap)
        dst3 = palette.dst2
        setSelectedContour(redCells, cellMap)
        labels(2) = $"FloodFill results with values varying from 0 to {classCount}"
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = RedCloud_Close(cPtr)
    End Sub
End Class






Public Class Flood_BasicsOld : Inherits VB_Algorithm
    Public classCount As Integer
    Public redC As New RedCloud_Basics
    Public Sub New()
        redOptions.UseColor.Checked = True
        labels(3) = "The flooded cells numbered from largest to smallast"
        desc = "FloodFill the input and paint it"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.cellMap
        dst3 = redC.dst2
        labels(2) = redC.labels(2)
        If standaloneTest() Then identifyCells(redC.redCells)
        classCount = redC.redCells.Count
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

        Dim rc As New rcData
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
    Public redCells As New List(Of rcData)
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
        Dim SizeSorted As New SortedList(Of Integer, rcData)(New compareAllowIdenticalIntegerInverted)
        Dim maskPlus = New cv.Mat(New cv.Size(dst2.Width + 2, dst2.Height + 2), cv.MatType.CV_8UC1, 0)
        Dim index = 1
        For Each pt In pointList
            Dim floodFlag = cv.FloodFillFlags.FixedRange Or (index << 8)
            Dim rc = New rcData
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
        Dim lastCells = New List(Of rcData)(redCells)
        cellMap.SetTo(0)
        redCells.Clear()
        redCells.Add(New rcData) ' stay away from 0...

        dst2.SetTo(0)
        Dim usedColors As New List(Of cv.Vec3b)({black})
        For i = 0 To SizeSorted.Count - 1
            Dim rc = SizeSorted.ElementAt(i).Value
            rc.index = redCells.Count
            Dim lrc = If(lastCells.Count > 0, lastCells(lastcellMap.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)), New rcData)
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
    Dim redC As New RedCloud_Basics
    Dim redCells As New List(Of rcData)
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

