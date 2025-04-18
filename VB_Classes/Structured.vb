Imports cv = OpenCvSharp
Public Class Structured_Basics : Inherits TaskParent
    Dim options As New Options_Structured
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Build structured slices through the point cloud."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        Dim stepsize = options.stepSize

        dst2.SetTo(0)
        Static startingOffset As Integer = 0
        Dim depthMask As New cv.Mat
        Dim minVal As Double, maxVal As Double
        For yCoordinate = startingOffset To src.Height - 1 Step stepsize
            Dim planeY = -task.yRange * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
            If yCoordinate > task.sideCameraPoint.Y Then planeY = task.yRange * (yCoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)
            minVal = planeY - task.metersPerPixel
            maxVal = planeY + task.metersPerPixel
            depthMask = task.pcSplit(1).InRange(minVal, maxVal)
            dst2.SetTo(255, depthMask)
            If minVal < 0 And maxVal > 0 Then dst2.SetTo(0, task.noDepthMask)
        Next

        dst3.SetTo(0)
        For xCoordinate = startingOffset To src.Width - 1 Step stepsize
            Dim planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
            If xCoordinate > task.topCameraPoint.X Then planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)
            minVal = planeX - task.metersPerPixel
            maxVal = planeX + task.metersPerPixel
            depthMask = task.pcSplit(0).InRange(minVal, maxVal)
            dst3.SetTo(255, depthMask)
            If minVal < 0 And maxVal > 0 Then dst3.SetTo(0, task.noDepthMask)
        Next
        startingOffset += 1
        If startingOffset >= stepsize Then startingOffset = 0
    End Sub
End Class





Public Class Structured_LinearizeFloor : Inherits TaskParent
    Public floor As New Structured_FloorCeiling
    Dim kalman As New Kalman_VB_Basics
    Public sliceMask As cv.Mat
    Public floorYPlane As Single
    Dim options As New Options_StructuredFloor
    Public Sub New()
        desc = "Using the mask for the floor create a better representation of the floor plane"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        floor.Run(src)
        dst2 = floor.dst2
        dst3 = floor.dst3
        sliceMask = floor.slice.sliceMask

        Dim imuPC = task.pointCloud.Clone
        imuPC.SetTo(0, Not sliceMask)

        If sliceMask.CountNonZero > 0 Then
            Dim split = imuPC.Split()
            If options.xCheck Then
                Dim mm As mmData = GetMinMax(split(0), sliceMask)

                Dim firstCol As Integer, lastCol As Integer
                For firstCol = 0 To sliceMask.Width - 1
                    If sliceMask.Col(firstCol).CountNonZero > 0 Then Exit For
                Next
                For lastCol = sliceMask.Width - 1 To 0 Step -1
                    If sliceMask.Col(lastCol).CountNonZero Then Exit For
                Next

                Dim xIncr = (mm.maxVal - mm.minVal) / (lastCol - firstCol)
                For i = firstCol To lastCol
                    Dim maskCol = sliceMask.Col(i)
                    If maskCol.CountNonZero > 0 Then split(0).Col(i).SetTo(mm.minVal + xIncr * i, maskCol)
                Next
            End If

            If options.yCheck Then
                Dim mm As mmData = GetMinMax(split(1), sliceMask)
                kalman.kInput = (mm.minVal + mm.maxVal) / 2
                kalman.Run(src)
                floorYPlane = kalman.kAverage
                split(1).SetTo(floorYPlane, sliceMask)
            End If

            If options.zCheck Then
                Dim firstRow As Integer, lastRow As Integer
                For firstRow = 0 To sliceMask.Height - 1
                    If sliceMask.Row(firstRow).CountNonZero > 20 Then Exit For
                Next
                For lastRow = sliceMask.Height - 1 To 0 Step -1
                    If sliceMask.Row(lastRow).CountNonZero > 20 Then Exit For
                Next

                If lastRow >= 0 And firstRow < sliceMask.Height Then
                    Dim meanMin = split(2).Row(lastRow).Mean(sliceMask.Row(lastRow))
                    Dim meanMax = split(2).Row(firstRow).Mean(sliceMask.Row(firstRow))
                    Dim zIncr = (meanMax(0) - meanMin(0)) / Math.Abs(lastRow - firstRow)
                    For i = firstRow To lastRow
                        Dim maskRow = sliceMask.Row(i)
                        Dim mean = split(2).Row(i).Mean(maskRow)
                        If maskRow.CountNonZero > 0 Then
                            split(2).Row(i).SetTo(mean(0))
                        End If
                    Next
                    dst2.Line(New cv.Point(0, firstRow), New cv.Point(dst2.Width, firstRow), cv.Scalar.Yellow, task.lineWidth + 1)
                    dst2.Line(New cv.Point(0, lastRow), New cv.Point(dst2.Width, lastRow), cv.Scalar.Yellow, task.lineWidth + 1)
                End If
            End If

            cv.Cv2.Merge(split, imuPC)

            imuPC.CopyTo(task.pointCloud, sliceMask)
        End If
    End Sub
End Class







Public Class Structured_MultiSliceLines : Inherits TaskParent
    Dim multi As New Structured_MultiSlice
    Dim lines As New Line_BasicsRaw
    Public Sub New()
        desc = "Detect lines in the multiSlice output"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        multi.Run(src)
        dst3 = multi.dst3
        lines.Run(dst3)
        dst2 = lines.dst2
        labels(2) = lines.labels(2)
    End Sub
End Class






Public Class Structured_Depth : Inherits TaskParent
    Dim sliceH As New Structured_SliceH
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"", "", "Use mouse to explore slices", "Top down view of the highlighted slice (at left)"}
        desc = "Use the structured depth to enhance the depth away from the centerline."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        sliceH.Run(src)
        dst0 = sliceH.dst3
        dst2 = sliceH.dst2

        Dim mask = sliceH.sliceMask
        Dim perMeter = dst3.Height / task.MaxZmeters
        dst3.SetTo(0)
        Dim white As New cv.Vec3b(255, 255, 255)
        For y = 0 To mask.Height - 1
            For x = 0 To mask.Width - 1
                Dim val = mask.Get(Of Byte)(y, x)
                If val > 0 Then
                    Dim depth = task.pcSplit(2).Get(Of Single)(y, x)
                    Dim row = dst1.Height - depth * perMeter
                    dst3.Set(Of cv.Vec3b)(If(row < 0, 0, row), x, white)
                End If
            Next
        Next
    End Sub
End Class







Public Class Structured_Rebuild : Inherits TaskParent
    Dim heat As New HeatMap_Basics
    Dim options As New Options_Structured
    Dim thickness As Single
    Public pointcloud As New cv.Mat
    Public Sub New()
        labels = {"", "", "X values in point cloud", "Y values in point cloud"}
        desc = "Rebuild the point cloud using inrange - not useful yet"
    End Sub
    Private Function rebuildX(viewX As cv.Mat) As cv.Mat
        Dim output As New cv.Mat(task.pcSplit(1).Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim firstCol As Integer
        For firstCol = 0 To viewX.Width - 1
            If viewX.Col(firstCol).CountNonZero > 0 Then Exit For
        Next

        Dim lastCol As Integer
        For lastCol = viewX.Height - 1 To 0 Step -1
            If viewX.Row(lastCol).CountNonZero > 0 Then Exit For
        Next

        Dim sliceMask As New cv.Mat
        For i = firstCol To lastCol
            Dim planeX = -task.xRange * (task.topCameraPoint.X - i) / task.topCameraPoint.X
            If i > task.topCameraPoint.X Then planeX = task.xRange * (i - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)

            cv.Cv2.InRange(task.pcSplit(0), planeX - thickness, planeX + thickness, sliceMask)
            output.SetTo(planeX, sliceMask)
        Next
        Return output
    End Function
    Private Function rebuildY(viewY As cv.Mat) As cv.Mat
        Dim output As New cv.Mat(task.pcSplit(1).Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        Dim firstLine As Integer
        For firstLine = 0 To viewY.Height - 1
            If viewY.Row(firstLine).CountNonZero > 0 Then Exit For
        Next

        Dim lastLine As Integer
        For lastLine = viewY.Height - 1 To 0 Step -1
            If viewY.Row(lastLine).CountNonZero > 0 Then Exit For
        Next

        Dim sliceMask As New cv.Mat
        For i = firstLine To lastLine
            Dim planeY = -task.yRange * (task.sideCameraPoint.Y - i) / task.sideCameraPoint.Y
            If i > task.sideCameraPoint.Y Then planeY = task.yRange * (i - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)

            cv.Cv2.InRange(task.pcSplit(1), planeY - thickness, planeY + thickness, sliceMask)
            output.SetTo(planeY, sliceMask)
        Next
        Return output
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        Dim metersPerPixel = task.MaxZmeters / dst3.Height
        thickness = options.sliceSize * metersPerPixel
        heat.Run(src)

        If options.rebuilt Then
            task.pcSplit(0) = rebuildX(heat.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            task.pcSplit(1) = rebuildY(heat.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            cv.Cv2.Merge(task.pcSplit, pointcloud)
        Else
            task.pcSplit = task.pointCloud.Split()
            pointcloud = task.pointCloud
        End If

        dst2 = Convert32f_To_8UC3(task.pcSplit(0))
        dst3 = Convert32f_To_8UC3(task.pcSplit(1))
        dst2.SetTo(0, task.noDepthMask)
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class Structured_Cloud2 : Inherits TaskParent
    Dim mmPixel As New Pixel_Measure
    Dim options As New Options_StructuredCloud
    Public Sub New()
        desc = "Attempt to impose a structure on the point cloud data."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        Dim stepX = dst2.Width / options.xLines
        Dim stepY = dst2.Height / options.yLines
        dst3 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC3, 0)
        Dim midX = dst2.Width / 2
        Dim midY = dst2.Height / 2
        Dim halfStepX = stepX / 2
        Dim halfStepy = stepY / 2
        For y = 1 To options.yLines - 2
            For x = 1 To options.xLines - 2
                Dim p1 = New cv.Point2f(x * stepX, y * stepY)
                Dim p2 = New cv.Point2f((x + 1) * stepX, y * stepY)
                Dim d1 = task.pcSplit(2).Get(Of Single)(p1.Y, p1.X)
                Dim d2 = task.pcSplit(2).Get(Of Single)(p2.Y, p2.X)
                If stepX * options.threshold > Math.Abs(d1 - d2) And d1 > 0 And d2 > 0 Then
                    Dim p = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                    Dim mmPP = mmPixel.Compute(d1)
                    If options.xConstraint Then
                        p(0) = (p1.X - midX) * mmPP
                        If p1.X = midX Then p(0) = mmPP
                    End If
                    If options.yConstraint Then
                        p(1) = (p1.Y - midY) * mmPP
                        If p1.Y = midY Then p(1) = mmPP
                    End If
                    Dim r = New cv.Rect(p1.X - halfStepX, p1.Y - halfStepy, stepX, stepY)
                    Dim meanVal = cv.Cv2.Mean(task.pcSplit(2)(r), task.depthMask(r))
                    p(2) = (d1 + d2) / 2
                    dst3.Set(Of cv.Vec3f)(y, x, p)
                End If
            Next
        Next
        dst2 = dst3(New cv.Rect(0, 0, options.xLines, options.yLines)).Resize(dst2.Size(), 0, 0,
                                                                              cv.InterpolationFlags.Nearest)
    End Sub
End Class








Public Class Structured_Cloud : Inherits TaskParent
    Public options As New Options_StructuredCloud
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Attempt to impose a linear structure on the pointcloud."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        Dim yLines = CInt(options.xLines * dst2.Height / dst2.Width)

        Dim stepX = dst3.Width / options.xLines
        Dim stepY = dst3.Height / yLines
        dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
        For y = 0 To yLines - 1
            For x = 0 To options.xLines - 1
                Dim r = New cv.Rect(x * stepX, y * stepY, stepX - 1, stepY - 1)
                Dim p1 = New cv.Point(r.X, r.Y)
                Dim p2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
                Dim vec1 = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                Dim vec2 = task.pointCloud.Get(Of cv.Vec3f)(p2.Y, p2.X)
                If vec1(2) > 0 And vec2(2) > 0 Then dst2(r).SetTo(vec1)
            Next
        Next
        labels(2) = "Structured_Cloud with " + CStr(yLines) + " rows " + CStr(options.xLines) + " columns"
    End Sub
End Class







Public Class Structured_ROI : Inherits TaskParent
    Public data As New cv.Mat
    Public oglData As New List(Of cv.Point3f)
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Simplify the point cloud so it can be represented as quads in OpenGL"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
        For Each roi In task.gridRects
            Dim d = task.pointCloud(roi).Mean(task.depthMask(roi))
            Dim depth = New cv.Vec3f(d.Val0, d.Val1, d.Val2)
            Dim pt = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            Dim vec = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            If vec(2) > 0 Then dst2(roi).SetTo(depth)
        Next

        labels(2) = traceName + " with " + CStr(task.gridRects.Count) + " regions was created"
    End Sub
End Class







Public Class Structured_Tiles : Inherits TaskParent
    Public oglData As New List(Of cv.Vec3f)
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        task.gOptions.GridSlider.Value = 10
        desc = "Use the OpenGL point size to represent the point cloud as data"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst3

        dst3.SetTo(0)
        oglData.Clear()
        For Each roi In task.gridRects
            Dim c = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
            If c = black Then Continue For
            oglData.Add(New cv.Vec3f(c(2) / 255, c(1) / 255, c(0) / 255))

            Dim v = task.pointCloud(roi).Mean(task.depthMask(roi))
            oglData.Add(New cv.Vec3f(v.Val0, v.Val1, v.Val2))
            dst3(roi).SetTo(c)
        Next
        labels(2) = traceName + " with " + CStr(task.gridRects.Count) + " regions was created"
    End Sub
End Class






Public Class Structured_CountTop : Inherits TaskParent
    Dim slice As New Structured_SliceV
    Dim plot As New Plot_Histogram
    Dim counts As New List(Of Single)
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"", "Structured Slice heatmap input - red line is max", "Max Slice output - likely vertical surface", "Histogram of pixel counts in each slice"}
        desc = "Count the number of pixels found in each slice of the point cloud data."
    End Sub
    Private Function makeXSlice(index As Integer) As cv.Mat
        Dim sliceMask As New cv.Mat

        Dim planeX = -task.xRange * (task.topCameraPoint.X - index) / task.topCameraPoint.X
        If index > task.topCameraPoint.X Then planeX = task.xRange * (index - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)

        Dim minVal = planeX - task.metersPerPixel
        Dim maxVal = planeX + task.metersPerPixel
        cv.Cv2.InRange(task.pcSplit(0).Clone, minVal, maxVal, sliceMask)
        If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask) ' don't include zero depth locations
        counts.Add(sliceMask.CountNonZero)
        Return sliceMask
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        slice.Run(src)
        dst1 = slice.dst3.Clone

        counts.Clear()
        For i = 0 To dst2.Width - 1
            makeXSlice(i)
        Next

        Dim max = counts.Max
        Dim index = counts.IndexOf(max)
        dst0 = makeXSlice(index)
        dst2 = task.color.Clone
        dst2.SetTo(white, dst0)
        dst1.Line(New cv.Point(index, 0), New cv.Point(index, dst1.Height), cv.Scalar.Red, slice.options.sliceSize)

        Dim hist As cv.Mat = cv.Mat.FromPixelData(dst0.Width, 1, cv.MatType.CV_32F, counts.ToArray)
        plot.Run(hist)

        dst3 = plot.dst2
    End Sub
End Class







Public Class Structured_FloorCeiling : Inherits TaskParent
    Public slice As New Structured_SliceEither
    Public Sub New()
        ReDim task.kalman.kInput(2 - 1)
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = False
        desc = "Find the floor or ceiling plane"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        slice.Run(src)
        dst2 = slice.heat.dst3

        Dim floorMax As Single
        Dim floorY As Integer
        Dim floorBuffer = dst2.Height / 4
        For i = dst2.Height - 1 To 0 Step -1
            Dim nextSum = slice.heat.dst3.Row(i).Sum()(0)
            If nextSum > 0 Then floorBuffer -= 1
            If floorBuffer = 0 Then Exit For
            If nextSum > floorMax Then
                floorMax = nextSum
                floorY = i
            End If
        Next

        Dim ceilingMax As Single
        Dim ceilingY As Integer
        Dim ceilingBuffer = dst2.Height / 4
        For i = 0 To dst3.Height - 1
            Dim nextSum = slice.heat.dst3.Row(i).Sum()(0)
            If nextSum > 0 Then ceilingBuffer -= 1
            If ceilingBuffer = 0 Then Exit For
            If nextSum > ceilingMax Then
                ceilingMax = nextSum
                ceilingY = i
            End If
        Next

        task.kalman.kInput(0) = floorY
        task.kalman.kInput(1) = ceilingY
        task.kalman.Run(src)

        labels(2) = "Current slice is at row =" + CStr(task.mouseMovePoint.Y)
        labels(3) = "Ceiling is at row =" + CStr(CInt(task.kalman.kOutput(1))) + " floor at y=" + CStr(CInt(task.kalman.kOutput(0)))

        DrawLine(dst2, New cv.Point(0, floorY), New cv.Point(dst2.Width, floorY), cv.Scalar.Yellow)
        SetTrueText("floor", New cv.Point(10, floorY + task.DotSize), 3)

        Dim rect = New cv.Rect(0, Math.Max(ceilingY - 5, 0), dst2.Width, 10)
        Dim mask = slice.heat.dst3(rect)
        Dim mean As cv.Scalar, stdev As cv.Scalar
        cv.Cv2.MeanStdDev(mask, mean, stdev)
        If mean(0) < mean(2) Then
            DrawLine(dst2, New cv.Point(0, ceilingY), New cv.Point(dst2.Width, ceilingY), cv.Scalar.Yellow)
            SetTrueText("ceiling", New cv.Point(10, ceilingY + task.DotSize), 3)
        Else
            SetTrueText("Ceiling does not appear to be present", 3)
        End If
    End Sub
End Class







Public Class Structured_MultiSliceH : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public sliceMask As cv.Mat
    Dim options As New Options_Structured
    Public Sub New()
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = False
        desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        Dim stepsize = options.stepSize

        heat.Run(src)
        dst3 = heat.dst3

        sliceMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = -task.yRange * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
            If yCoordinate > task.sideCameraPoint.Y Then planeY = task.yRange * (yCoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)
            Dim depthMask As New cv.Mat
            Dim minVal As Double, maxVal As Double
            minVal = planeY - task.metersPerPixel
            maxVal = planeY + task.metersPerPixel
            cv.Cv2.InRange(task.pcSplit(1).Clone, minVal, maxVal, depthMask)
            sliceMask.SetTo(255, depthMask)
            If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask)
        Next

        dst2 = task.color.Clone
        dst2.SetTo(white, sliceMask)
        labels(3) = heat.labels(3)
    End Sub
End Class






Public Class Structured_SliceXPlot : Inherits TaskParent
    Dim multi As New Structured_MultiSlice
    Dim options As New Options_Structured
    Public Sub New()
        desc = "Find any plane around a peak value in the top-down histogram"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        multi.Run(src)
        dst3 = multi.heat.dst2

        Dim col = If(task.mouseMovePoint.X = 0, dst2.Width / 2, task.mouseMovePoint.X)

        Dim rect = New cv.Rect(col, 0, If(col + options.sliceSize >= dst3.Width, dst3.Width - col,
                               options.sliceSize), dst3.Height - 1)
        Dim mm As mmData = GetMinMax(multi.heat.topframes.dst2(rect))

        DrawCircle(dst3, New cv.Point(col, mm.maxLoc.Y), task.DotSize + 3, cv.Scalar.Yellow)

        dst2 = task.color.Clone
        Dim filterZ = (dst3.Height - mm.maxLoc.Y) / dst3.Height * task.MaxZmeters
        If filterZ > 0 Then
            Dim depthMask = task.pcSplit(2).InRange(filterZ - 0.05, filterZ + 0.05) ' a 10 cm buffer surrounding the z value
            dst2.SetTo(white, depthMask)
        End If

        labels(3) = "Peak histogram count (" + Format(mm.maxVal, fmt0) + ") at " + Format(filterZ, fmt2) + " meters +-" + Format(5 / dst2.Height / task.MaxZmeters, fmt2) + " m"
        SetTrueText("Use the mouse to move the yellow dot above.", New cv.Point(10, dst2.Height * 7 / 8), 3)
    End Sub
End Class







Public Class Structured_SliceYPlot : Inherits TaskParent
    Dim multi As New Structured_MultiSlice
    Dim options As New Options_Structured
    Public Sub New()
        desc = "Find any plane around a peak value in the side view histogram"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        multi.Run(src)
        dst3 = multi.heat.dst3

        Dim row = If(task.mouseMovePoint.Y = 0, dst2.Height / 2, task.mouseMovePoint.Y)

        Dim rect = New cv.Rect(0, row, dst3.Width - 1, If(row + options.sliceSize >= dst3.Height,
                               dst3.Height - row, options.sliceSize))
        Dim mm As mmData = GetMinMax(multi.heat.sideframes.dst2(rect))

        If mm.maxVal > 0 Then
            DrawCircle(dst3, New cv.Point(mm.maxLoc.X, row), task.DotSize + 3, cv.Scalar.Yellow)
            ' dst3.Line(New cv.Point(mm.maxLoc.X, 0), New cv.Point(mm.maxLoc.X, dst3.Height), task.highlight, task.lineWidth, task.lineType)
            Dim filterZ = mm.maxLoc.X / dst3.Width * task.MaxZmeters

            Dim depthMask = task.pcSplit(2).InRange(filterZ - 0.05, filterZ + 0.05) ' a 10 cm buffer surrounding the z value
            dst2 = task.color.Clone
            dst2.SetTo(white, depthMask)
            Dim pixelsPerMeter = dst2.Width / task.MaxZmeters
            labels(3) = "Peak histogram count (" + Format(mm.maxVal, fmt0) + ") at " + Format(filterZ, fmt2) + " meters +-" + Format(5 / pixelsPerMeter, fmt2) + " m"
        End If
        SetTrueText("Use the mouse to move the yellow dot above.", New cv.Point(10, dst2.Height * 7 / 8), 3)
    End Sub
End Class








Public Class Structured_MouseSlice : Inherits TaskParent
    Dim slice As New Structured_SliceEither
    Dim lines As New Line_BasicsRaw
    Public Sub New()
        labels(2) = "Center Slice in yellow"
        labels(3) = "White = SliceV output, Red Dot is avgPt"
        desc = "Find the vertical center line with accurate depth data.."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If task.mouseMovePoint = newPoint Then task.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
        slice.Run(src)

        lines.Run(slice.sliceMask)
        Dim tops As New List(Of Integer)
        Dim bots As New List(Of Integer)
        Dim topsList As New List(Of cv.Point)
        Dim botsList As New List(Of cv.Point)
        If task.lpList.Count > 0 Then
            dst3 = lines.dst2
            For Each lp In task.lpList
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 3, task.lineType)
                tops.Add(If(lp.p1.Y < lp.p2.Y, lp.p1.Y, lp.p2.Y))
                bots.Add(If(lp.p1.Y > lp.p2.Y, lp.p1.Y, lp.p2.Y))
                topsList.Add(lp.p1)
                botsList.Add(lp.p2)
            Next

            'Dim topPt = topsList(tops.IndexOf(tops.Min))
            'Dim botPt = botsList(bots.IndexOf(bots.Max))
            'DrawCircle(dst3,New cv.Point2f((topPt.X + botPt.X) / 2, (topPt.Y + botPt.Y) / 2), task.DotSize + 5, cv.Scalar.Red)
            'dst3.Line(topPt, botPt, cv.Scalar.Red, task.lineWidth, task.lineType)
            'DrawLine(dst2,topPt, botPt, task.highlight, task.lineWidth + 2, task.lineType)
        End If
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(white, dst3)
        End If
    End Sub
End Class







Public Class Structured_SliceEither : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public sliceMask As New cv.Mat
    Dim options As New Options_Structured
    Public Sub New()
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = False
        desc = "Create slices in top and side views"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        Static topRadio = optiBase.FindCheckBox("Top View (Unchecked Side View)")
        Dim topView = topRadio.checked

        Dim sliceVal = If(topView, task.mouseMovePoint.X, task.mouseMovePoint.Y)
        heat.Run(src)

        Dim minVal As Double, maxVal As Double
        If topView Then
            Dim planeX = -task.xRange * (task.topCameraPoint.X - sliceVal) / task.topCameraPoint.X
            If sliceVal > task.topCameraPoint.X Then planeX = task.xRange * (sliceVal - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)
            minVal = planeX - task.metersPerPixel
            maxVal = planeX + task.metersPerPixel
            sliceMask = task.pcSplit(0).InRange(minVal, maxVal)
        Else
            Dim planeY = -task.yRange * (task.sideCameraPoint.Y - sliceVal) / task.sideCameraPoint.Y
            If sliceVal > task.sideCameraPoint.Y Then planeY = task.yRange * (sliceVal - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)
            minVal = planeY - task.metersPerPixel
            maxVal = planeY + task.metersPerPixel
            sliceMask = task.pcSplit(1).InRange(minVal, maxVal)
        End If

        If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask)

        labels(2) = "At offset " + CStr(sliceVal) + " x = " + Format((maxVal + minVal) / 2, fmt2) + " with " +
                 Format(Math.Abs(maxVal - minVal) * 100, fmt2) + " cm width"

        labels(3) = heat.labels(3)

        dst3 = heat.dst3
        DrawCircle(dst3, New cv.Point(task.topCameraPoint.X, dst3.Height), task.DotSize,
                    cv.Scalar.Yellow)
        If topView Then
            dst3.Line(New cv.Point(sliceVal, 0), New cv.Point(sliceVal, dst3.Height),
                      cv.Scalar.Yellow, task.lineWidth)
        Else
            Dim yPlaneOffset = If(sliceVal < dst3.Height - options.sliceSize, CInt(sliceVal),
                                  dst3.Height - options.sliceSize - 1)
            dst3.Line(New cv.Point(0, yPlaneOffset), New cv.Point(dst3.Width, yPlaneOffset), cv.Scalar.Yellow,
                      options.sliceSize)
        End If
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(white, sliceMask)
        End If
    End Sub
End Class










Public Class Structured_TransformH : Inherits TaskParent
    Dim options As New Options_Structured
    Dim histTop As New Projection_HistTop
    Public Sub New()
        labels(3) = "Top down view of the slice of the point cloud"
        desc = "Find and isolate planes (floor and ceiling) in a TopView or SideView histogram."
    End Sub
    Public Function createSliceMaskH() As cv.Mat
        options.Run()

        Dim sliceMask As New cv.Mat
        Dim ycoordinate = If(task.mouseMovePoint.Y = 0, dst2.Height / 2, task.mouseMovePoint.Y)

        Dim planeY = -task.yRange * (task.sideCameraPoint.Y - ycoordinate) / task.sideCameraPoint.Y
        If ycoordinate > task.sideCameraPoint.Y Then planeY = task.yRange * (ycoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)

        Dim thicknessMeters = options.sliceSize * task.metersPerPixel
        Dim minVal = planeY - thicknessMeters
        Dim maxVal = planeY + thicknessMeters
        cv.Cv2.InRange(task.pcSplit(1), minVal, maxVal, sliceMask)

        labels(2) = "At offset " + CStr(ycoordinate) + " y = " + Format((maxVal + minVal) / 2, fmt2) + " with " +
                    Format(Math.Abs(maxVal - minVal) * 100, fmt2) + " cm width"
        If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask)

        Return sliceMask
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim sliceMask = createSliceMaskH()

        histTop.Run(task.pointCloud.SetTo(0, Not sliceMask))
        dst3 = histTop.dst2

        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(white, sliceMask)
        End If
    End Sub
End Class






Public Class Structured_TransformV : Inherits TaskParent
    Dim options As New Options_Structured
    Dim histSide As New Projection_HistSide
    Public Sub New()
        labels(3) = "Side view of the slice of the point cloud"
        desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Function createSliceMaskV() As cv.Mat
        options.Run()

        Dim sliceMask As New cv.Mat
        If task.mouseMovePoint = newPoint Then task.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
        Dim xCoordinate = If(task.mouseMovePoint.X = 0, dst2.Width / 2, task.mouseMovePoint.X)

        Dim planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
        If xCoordinate > task.topCameraPoint.X Then planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)

        Dim thicknessMeters = options.sliceSize * task.metersPerPixel
        Dim minVal = planeX - thicknessMeters
        Dim maxVal = planeX + thicknessMeters
        cv.Cv2.InRange(task.pcSplit(0), minVal, maxVal, sliceMask)

        labels(2) = "At offset " + CStr(xCoordinate) + " x = " + Format((maxVal + minVal) / 2, fmt2) + " with " +
                    Format(Math.Abs(maxVal - minVal) * 100, fmt2) + " cm width"

        If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask)

        Return sliceMask
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim sliceMask = createSliceMaskV()

        histSide.Run(task.pointCloud.SetTo(0, Not sliceMask))
        dst3 = histSide.dst2

        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(white, sliceMask)
        End If
    End Sub
End Class






Public Class Structured_CountSide : Inherits TaskParent
    Dim slice As New Structured_SliceH
    Dim plot As New Plot_Histogram
    Dim rotate As New Rotate_Basics
    Public counts As New List(Of Single)
    Public maxCountIndex As Integer
    Public yValues As New List(Of Single)
    Public Sub New()
        rotate.rotateCenter = New cv.Point2f(dst2.Width / 2, dst2.Width / 2)
        rotate.rotateAngle = -90
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"", "Max Slice output - likely flat surface", "Structured Slice heatmap input - red line is max", "Histogram of pixel counts in each slice"}
        desc = "Count the number of pixels found in each slice of the point cloud data."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        slice.Run(src)
        dst2 = slice.dst3

        counts.Clear()
        yValues.Clear()
        For i = 0 To dst2.Height - 1
            Dim planeY = task.yRange * (i - task.sideCameraPoint.Y) / task.sideCameraPoint.Y
            Dim minVal = planeY - task.metersPerPixel, maxVal = planeY + task.metersPerPixel

            Dim sliceMask = task.pcSplit(1).InRange(minVal, maxVal)
            If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask) ' don't include zero depth locations
            counts.Add(sliceMask.CountNonZero)
            yValues.Add(planeY)
        Next

        Dim max = counts.Max
        maxCountIndex = counts.IndexOf(max)
        dst2.Line(New cv.Point(0, maxCountIndex), New cv.Point(dst2.Width, maxCountIndex), cv.Scalar.Red, slice.options.sliceSize)

        Dim hist As cv.Mat = cv.Mat.FromPixelData(dst0.Height, 1, cv.MatType.CV_32F, counts.ToArray)
        plot.dst2 = New cv.Mat(dst2.Height, dst2.Height, cv.MatType.CV_8UC3, cv.Scalar.All(0))
        plot.Run(hist)
        dst3 = plot.dst2

        dst3 = dst3.Resize(New cv.Size(dst2.Width, dst2.Width))
        rotate.Run(dst3)
        dst3 = rotate.dst2
        SetTrueText("Max flat surface at: " + vbCrLf + Format(yValues(maxCountIndex), fmt3), 2)
    End Sub
End Class





Public Class Structured_CountSideSum : Inherits TaskParent
    Public counts As New List(Of Single)
    Public maxCountIndex As Integer
    Public yValues As New List(Of Single)
    Public Sub New()
        task.redOptions.ProjectionThresholdBar.Value += 50 ' to get the point cloud into the histogram.
        labels = {"", "Max Slice output - likely flat surface", "Structured Slice heatmap input - red line is max", "Histogram of pixel counts in each slice"}
        desc = "Count the number of points found in each slice of the point cloud data."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        cv.Cv2.CalcHist({task.pointCloud}, task.channelsSide, New cv.Mat, dst2, 2, task.bins2D, task.rangesSide)
        dst2.Col(0).SetTo(0)

        counts.Clear()
        yValues.Clear()
        Dim ratio = task.yRange / task.yRangeDefault
        For i = 0 To dst2.Height - 1
            Dim planeY = task.yRange * (i - task.sideCameraPoint.Y) / task.sideCameraPoint.Y
            counts.Add(dst2.Row(i).Sum(0))
            yValues.Add(planeY * ratio)
        Next

        dst2 = dst2.Threshold(0, cv.Scalar.White, cv.ThresholdTypes.Binary)

        Dim max = counts.Max
        If max = 0 Then Exit Sub

        Dim surfaces As New List(Of Single)
        For i = 0 To counts.Count - 1
            If counts(i) >= max / 2 Then
                DrawLine(dst2, New cv.Point(0, i), New cv.Point(dst2.Width, i), white)
                surfaces.Add(yValues(i))
            End If
        Next

        If task.heartBeat Then
            strOut = "Flat surface at: "
            For i = 0 To surfaces.Count - 1
                strOut += Format(surfaces(i), fmt3) + ", "
                If i Mod 10 = 0 And i > 0 Then strOut += vbCrLf
            Next
        End If
        SetTrueText(strOut, 2)

        dst3.SetTo(cv.Scalar.Red)
        Dim barHeight = dst2.Height / counts.Count
        For i = 0 To counts.Count - 1
            Dim w = dst2.Width * counts(i) / max
            cv.Cv2.Rectangle(dst3, New cv.Rect(0, i * barHeight, w, barHeight), cv.Scalar.Black, -1)
        Next
    End Sub
End Class








Public Class Structured_SliceV : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public sliceMask As New cv.Mat
    Public options As New Options_Structured
    Public Sub New()
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = True
        desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        If task.mouseMovePoint = newPoint Then task.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
        Dim xCoordinate = If(task.mouseMovePoint.X = 0, dst2.Width / 2, task.mouseMovePoint.X)

        heat.Run(src)

        Dim planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
        If xCoordinate > task.topCameraPoint.X Then planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)

        Dim thicknessMeters = options.sliceSize * task.metersPerPixel
        Dim minVal = planeX - thicknessMeters
        Dim maxVal = planeX + thicknessMeters
        cv.Cv2.InRange(task.pcSplit(0), minVal, maxVal, sliceMask)
        If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask)

        labels(2) = "At offset " + CStr(xCoordinate) + " x = " + Format((maxVal + minVal) / 2, fmt2) +
                    " with " + Format(Math.Abs(maxVal - minVal) * 100, fmt2) + " cm width"

        labels(3) = heat.labels(3)

        dst3 = heat.dst2
        DrawCircle(dst3, New cv.Point(task.topCameraPoint.X, 0), task.DotSize, task.highlight)
        dst3.Line(New cv.Point(xCoordinate, 0), New cv.Point(xCoordinate, dst3.Height), task.highlight, options.sliceSize)
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(white, sliceMask)
        End If
    End Sub
End Class







Public Class Structured_SliceH : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public sliceMask As New cv.Mat
    Public options As New Options_Structured
    Public ycoordinate As Integer
    Public Sub New()
        desc = "Find and isolate planes (floor and ceiling) in a TopView or SideView histogram."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        heat.Run(src)

        If standaloneTest() Then ycoordinate = If(task.mouseMovePoint.Y = 0, dst2.Height / 2, task.mouseMovePoint.Y)

        Dim sliceY = -task.yRange * (task.sideCameraPoint.Y - ycoordinate) / task.sideCameraPoint.Y
        If ycoordinate > task.sideCameraPoint.Y Then sliceY = task.yRange * (ycoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)

        Dim thicknessMeters = options.sliceSize * task.metersPerPixel
        Dim minVal = sliceY - thicknessMeters
        Dim maxVal = sliceY + thicknessMeters
        cv.Cv2.InRange(task.pcSplit(1), minVal, maxVal, sliceMask)

        labels(2) = "At offset " + CStr(ycoordinate) + " y = " + Format((maxVal + minVal) / 2, fmt2) +
                    " with " + Format(Math.Abs(maxVal - minVal) * 100, fmt2) + " cm width"
        If minVal <= 0 And maxVal >= 0 Then sliceMask.SetTo(0, task.noDepthMask)
        labels(3) = heat.labels(2)

        dst3 = heat.dst3
        Dim yPlaneOffset = If(ycoordinate < dst3.Height - options.sliceSize, CInt(ycoordinate), dst3.Height - options.sliceSize - 1)
        DrawCircle(dst3, New cv.Point(0, task.sideCameraPoint.Y), task.DotSize, task.highlight)
        dst3.Line(New cv.Point(0, yPlaneOffset), New cv.Point(dst3.Width, yPlaneOffset), task.highlight, options.sliceSize)
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(white, sliceMask)
        End If
    End Sub
End Class







Public Class Structured_SurveyH : Inherits TaskParent
    Public Sub New()
        task.redOptions.YRangeSlider.Value = 300
        UpdateAdvice(traceName + ": use Y-Range slider in RedCloud options.")
        labels(2) = "Each slice represents point cloud pixels with the same Y-Range"
        labels(3) = "Y-Range - compressed to increase the size of each slice.  Use Y-range slider to adjust the size of each slice."
        desc = "Mark each horizontal slice with a separate color.  Y-Range determines how thick the slice is."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        cv.Cv2.CalcHist({src}, task.channelsSide, New cv.Mat, dst3, 2, task.bins2D, task.rangesSide)
        dst3.Col(0).SetTo(0)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst3.ConvertTo(dst3, cv.MatType.CV_8U)

        Dim topRow As Integer
        For topRow = 0 To dst2.Height - 1
            If dst3.Row(topRow).CountNonZero Then Exit For
        Next

        Dim botRow As Integer
        For botRow = dst2.Height - 1 To 0 Step -1
            If dst3.Row(botRow).CountNonZero Then Exit For
        Next

        Dim index As Integer
        dst2.SetTo(0)
        For y = topRow To botRow
            Dim sliceY = -task.yRange * (task.sideCameraPoint.Y - y) / task.sideCameraPoint.Y
            If y > task.sideCameraPoint.Y Then sliceY = task.yRange * (y - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)
            Dim minVal = sliceY - task.metersPerPixel
            Dim maxVal = sliceY + task.metersPerPixel
            If minVal < 0 And maxVal > 0 Then Continue For
            dst0 = task.pcSplit(1).InRange(minVal, maxVal)
            dst2.SetTo(task.scalarColors(index Mod 256), dst0)
            index += 1
        Next
    End Sub
End Class







Public Class Structured_SurveyV : Inherits TaskParent
    Public Sub New()
        task.redOptions.setXRangeSlider(250)
        UpdateAdvice(traceName + ": use X-Range slider in RedCloud options.")
        labels(2) = "Each slice represents point cloud pixels with the same X-Range"
        labels(3) = "X-Range - compressed to increase the size of each slice.  Use X-range slider to adjust the size of each slice."
        desc = "Mark each vertical slice with a separate color.  X-Range determines how thick the slice is."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        cv.Cv2.CalcHist({src}, task.channelsTop, New cv.Mat, dst3, 2, task.bins2D, task.rangesTop)
        dst3.Row(0).SetTo(0)
        dst3 = dst3.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst3.ConvertTo(dst3, cv.MatType.CV_8U)

        Dim column As Integer
        For column = 0 To dst2.Width - 1
            If dst3.Col(column).CountNonZero Then Exit For
        Next

        Dim lastColumn As Integer
        For lastColumn = dst2.Width - 1 To 0 Step -1
            If dst3.Col(lastColumn).CountNonZero Then Exit For
        Next

        Dim index As Integer
        dst2.SetTo(0)
        For x = column To lastColumn
            Dim sliceX = -task.xRange * (task.topCameraPoint.X - x) / task.topCameraPoint.X
            If x > task.topCameraPoint.X Then sliceX = task.xRange * (x - task.topCameraPoint.X) / (dst3.Height - task.topCameraPoint.X)
            Dim minVal = sliceX - task.metersPerPixel
            Dim maxVal = sliceX + task.metersPerPixel
            If minVal < 0 And maxVal > 0 Then Continue For
            dst0 = task.pcSplit(0).InRange(minVal, maxVal)
            dst2.SetTo(task.scalarColors(index Mod 256), dst0)
            index += 1
        Next
    End Sub
End Class








Public Class Structured_MultiSlicePolygon : Inherits TaskParent
    Dim multi As New Structured_MultiSlice
    Dim options As New Options_StructuredMulti
    Public Sub New()
        labels(2) = "Input to FindContours"
        labels(3) = "ApproxPolyDP 4-corner object from FindContours input"
        desc = "Detect polygons in the multiSlice output"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        multi.Run(src)
        dst2 = Not multi.dst3
        If dst2.Channels <> 1 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim rawContours = cv.Cv2.FindContoursAsArray(dst2, cv.RetrievalModes.Tree,
                                                      cv.ContourApproximationModes.ApproxSimple)
        Dim contours(rawContours.Length - 1)() As cv.Point
        For j = 0 To rawContours.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(rawContours(j), 3, True)
        Next

        dst3.SetTo(0)
        For i = 0 To contours.Length - 1
            If contours(i).Length = 2 Then Continue For
            If contours(i).Length <= options.maxSides Then
                cv.Cv2.DrawContours(dst3, contours, i, New cv.Scalar(0, 255, 255), task.lineWidth + 1, task.lineType)
            End If
        Next
    End Sub
End Class








Public Class Structured_Crosshairs : Inherits TaskParent
    Dim sCloud As New Structured_Cloud
    Dim minX As Single, maxX As Single, minY As Single, maxY As Single
    Public Sub New()
        desc = "Connect vertical and horizontal dots that are in the same column and row."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim xLines = sCloud.options.indexX
        Dim yLines = CInt(xLines * dst2.Width / dst2.Height)
        If sCloud.options.indexX > xLines Then sCloud.options.indexX = xLines - 1
        If sCloud.options.indexY > yLines Then sCloud.options.indexY = yLines - 1

        sCloud.Run(src)
        Dim split = cv.Cv2.Split(sCloud.dst2)

        Dim mmX = GetMinMax(split(0))
        Dim mmY = GetMinMax(split(1))

        minX = If(minX > mmX.minVal, mmX.minVal, minX)
        minY = If(minY > mmY.minVal, mmY.minVal, minY)
        maxX = If(maxX < mmX.maxVal, mmX.maxVal, maxX)
        maxY = If(maxY < mmY.maxVal, mmY.maxVal, maxY)

        SetTrueText("mmx min/max = " + Format(minX, "0.00") + "/" + Format(maxX, "0.00") + " mmy min/max " + Format(minY, "0.00") +
                    "/" + Format(maxY, "0.00"), 3)

        dst2.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        Dim pointX As New cv.Mat(sCloud.dst2.Size(), cv.MatType.CV_32S, 0)
        Dim pointY As New cv.Mat(sCloud.dst2.Size(), cv.MatType.CV_32S, 0)
        Dim yy As Integer, xx As Integer
        For y = 1 To sCloud.dst2.Height - 1
            For x = 1 To sCloud.dst2.Width - 1
                Dim p = sCloud.dst2.Get(Of cv.Vec3f)(y, x)
                If p(2) > 0 Then
                    If Single.IsNaN(p(0)) Or Single.IsNaN(p(1)) Or Single.IsNaN(p(2)) Then Continue For
                    xx = dst2.Width * (maxX - p(0)) / (maxX - minX)
                    yy = dst2.Height * (maxY - p(1)) / (maxY - minY)
                    If xx < 0 Then xx = 0
                    If yy < 0 Then yy = 0
                    If xx >= dst2.Width Then xx = dst2.Width - 1
                    If yy >= dst2.Height Then yy = dst2.Height - 1
                    yy = dst2.Height - yy - 1
                    xx = dst2.Width - xx - 1
                    dst2.Set(Of cv.Vec3b)(yy, xx, white)

                    pointX.Set(Of Integer)(y, x, xx)
                    pointY.Set(Of Integer)(y, x, yy)
                    If x = sCloud.options.indexX Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y - 1, x), pointY.Get(Of Integer)(y - 1, x))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst2.Line(p1, p2, task.highlight, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                    If y = sCloud.options.indexY Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y, x - 1), pointY.Get(Of Integer)(y, x - 1))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst2.Line(p1, p2, task.highlight, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                End If
            Next
        Next
    End Sub
End Class






Public Class Structured_MultiSlice : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Public sliceMask As cv.Mat
    Public options As New Options_Structured
    Public classCount As Integer
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        Dim stepSize = options.stepSize

        heat.Run(src)

        dst2.SetTo(0)
        classCount = 0
        Dim minVal As Double, maxVal As Double
        For xCoordinate = 0 To src.Width - 1 Step stepSize
            Dim planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
            If xCoordinate > task.topCameraPoint.X Then
                planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)
            End If
            minVal = planeX - task.metersPerPixel
            maxVal = planeX + task.metersPerPixel
            Dim depthMask = task.pcSplit(0).InRange(minVal, maxVal)
            dst2.SetTo(classCount, depthMask)
            classCount += 1
        Next

        For yCoordinate = 0 To src.Height - 1 Step stepSize
            Dim planeY = -task.yRange * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
            If yCoordinate > task.sideCameraPoint.Y Then
                planeY = task.yRange * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)
            End If
            minVal = planeY - task.metersPerPixel
            maxVal = planeY + task.metersPerPixel
            Dim depthMask = task.pcSplit(1).InRange(minVal, maxVal)
            dst2.SetTo(classCount, depthMask)
            classCount += 1
        Next

        dst3 = ShowPalette(dst2)
        labels(3) = "ClassCount = " + CStr(classCount)
    End Sub
End Class






Public Class Structured_MultiSliceV : Inherits TaskParent
    Public heat As New HeatMap_Basics
    Dim options As New Options_Structured
    Public Sub New()
        optiBase.FindCheckBox("Top View (Unchecked Side View)").Checked = True
        desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()
        Dim stepsize = options.stepSize

        heat.Run(src)
        dst3 = heat.dst2

        Dim sliceMask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
            If xCoordinate > task.topCameraPoint.X Then planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)
            Dim depthMask As New cv.Mat
            Dim minVal As Double, maxVal As Double
            minVal = planeX - task.metersPerPixel
            maxVal = planeX + task.metersPerPixel
            cv.Cv2.InRange(task.pcSplit(0).Clone, minVal, maxVal, depthMask)
            sliceMask.SetTo(255, depthMask)
            If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask)
        Next

        dst2 = task.color.Clone
        dst2.SetTo(white, sliceMask)
        labels(3) = heat.labels(3)
    End Sub
End Class








Public Class Structured_LinesX : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim lines As New Line_BasicsRaw
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Find the lines in the X-direction of the Structured_Basics output"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standalone Then
            Static struct As New Structured_Basics
            struct.Run(src)
            src = struct.dst2
        End If

        lines.Run(src)
        lpList = New List(Of lpData)(lines.lpList)

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(lpList.Count) + " lines found in X-direction slices"
    End Sub
End Class




Public Class Structured_LinesY : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim lines As New Line_BasicsRaw
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Find the lines in the Y-direction of the Structured_Basics output"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standalone Then
            Static struct As New Structured_Basics
            struct.Run(src)
            src = struct.dst3
        End If

        lines.Run(src)
        lpList = New List(Of lpData)(lines.lpList)

        dst2.SetTo(0)
        For Each lp In lpList
            dst2.Line(lp.p1, lp.p2, 255, task.lineWidth, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " lines found in Y-direction slices"
    End Sub
End Class
