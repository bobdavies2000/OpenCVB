Imports cv = OpenCvSharp
Public Class Flood_Basics : Inherits VB_Algorithm
    Public classCount As Integer
    Public rMin As New RedColor_Basics
    Public Sub New()
        labels(3) = "The flooded cells numbered from largest (1) to smallast (x < 255)"
        desc = "FloodFill the input and paint it"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst2
        dst3 = rMin.dst3
        labels(2) = rMin.labels(2)
        classCount = rMin.minCells.Count
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
        labels(3) = If(standalone, "Flood_Point standalone just shows the edges", "Resulting mask from floodfill")
        desc = "Use floodfill at a single location in a grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src.Clone()
        If standalone Then
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

        If task.optionsChanged Or standalone Then
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
        Dim minPixels = gOptions.minPixelsSlider.Value
        For Each pt In pointList
            Dim floodFlag = cv.FloodFillFlags.FixedRange Or (index << 8)
            Dim rc = New rcData
            rc.pixels = cv.Cv2.FloodFill(dst0, maskPlus, pt, 255, rc.rect, 0, 0, floodFlag)
            If rc.pixels >= minPixels And rc.rect.Width < dst2.Width And rc.rect.Height < dst2.Height And
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
            If usedColors.Contains(rc.color) Then
                rc.color = randomCellColor()
            End If

            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
            vbDrawContour(cellMap(rc.rect), rc.contour, rc.index, -1)

            redCells.Add(rc)
            usedColors.Add(rc.color)
        Next
    End Sub
End Class








Public Class Flood_Featureless : Inherits VB_Algorithm
    Public classCount As Integer
    Dim rMin As New RedColor_Basics
    Dim minCells As New List(Of segCell)
    Dim contour As New Contour_Basics
    Public Sub New()
        labels = {"", "", "", "Palette output of image at left"}
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "FloodFill the input and paint it with LUT"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If standalone Then
            Static fless As New FeatureLess_Basics
            fless.Run(src)
            src = fless.dst2
        End If

        rMin.Run(src)
        classCount = rMin.minCells.Count

        dst2.SetTo(0)
        minCells.Clear()
        For Each rp In rMin.minCells
            Dim contour = contourBuild(rp.mask, cv.ContourApproximationModes.ApproxNone) ' .ApproxTC89L1
            Dim hull = cv.Cv2.ConvexHull(contour, True).ToList
            vbDrawContour(dst2(rp.rect), hull, rp.index, -1)
            minCells.Add(rp)
        Next

        labels(2) = "Hulls were added for each of the " + CStr(minCells.Count) + " cells identified"
        dst3 = vbPalette(dst2 * 255 / minCells.Count)
    End Sub
End Class
