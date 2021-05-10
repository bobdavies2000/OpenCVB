Imports cv = OpenCvSharp
Public Class Structured_SliceOptions : Inherits VBparent
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 10, 1)
            sliders.setupTrackBar(1, "Slice step size in pixels (multi-slice option only)", 1, 100, 20)
        End If
        task.desc = "Structured Slice options"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        task.trueText("This algorithm is used to share the horizontal and vertical slice options.")
    End Sub
End Class







Public Class Structured_Floor : Inherits VBparent
    Public structD As New Structured_SliceH
    Dim kalman As New Kalman_VB_Basics
    Public floorYplane As Single
    Public Sub New()
        structD.cushionSlider.Value = 5 ' floor runs can use a thinner slice that ceilings...

        task.desc = "Find the floor plane"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        structD.Run(src)

        Dim yCoordinate = dst2.Height
        Dim lastSum = dst2.Row(dst2.Height - 1).Sum()
        For yCoordinate = dst2.Height - 1 To 0 Step -1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 1000 Then Exit For
            lastSum = nextSum
        Next

        kalman.kInput = yCoordinate
        kalman.Run(src)

        ' it settles down quicker...
        If task.frameCount > 30 Then yCoordinate = kalman.kAverage

        floorYplane = (task.maxY) * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)

        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class








Public Class Structured_Ceiling : Inherits VBparent
    Public structD As New Structured_SliceH
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(0)
        structD.cushionSlider.Value = 10
        task.desc = "Find the ceiling plane"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        structD.Run(src)

        Dim yCoordinate As Integer
        Dim lastSum = dst2.Row(yCoordinate).Sum()
        For yCoordinate = 1 To dst2.Height - 1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 1000 Then Exit For
            lastSum = nextSum
        Next

        kalman.kInput(0) = yCoordinate
        kalman.Run(src)

        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class Structured_MultiSliceH : Inherits VBparent
    Public side2D As New Histogram_SideData
    Public structD As New Structured_SliceH
    Public sliceMask As cv.Mat
    Public Sub New()
        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim cushion = cushionSlider.Value
        Dim stepsize = stepSlider.value

        side2D.Run(src)
        dst2 = side2D.dst2
        Dim Split = side2D.gCloud.dst1.Split()

        Dim metersPerPixel = task.maxZ / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        sliceMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = -task.maxY * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
            If yCoordinate > task.sideCameraPoint.Y Then planeY = task.maxY * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)
            Dim depthMask As New cv.Mat
            minVal = planeY - thicknessMeters
            maxVal = planeY + thicknessMeters
            cv.Cv2.InRange(Split(1).Clone, minVal, maxVal, depthMask)
            sliceMask.SetTo(255, depthMask)
            sliceMask.SetTo(0, task.noDepthMask)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = side2D.label2
    End Sub
End Class






Public Class Structured_MultiSliceV : Inherits VBparent
    Public top2D As New Histogram_TopData
    Public structD As New Structured_SliceV
    Public Sub New()
        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim cushion = cushionSlider.Value
        Dim stepsize = stepSlider.value

        top2D.Run(src)
        dst2 = top2D.dst2

        Dim split = top2D.gCloud.dst1.Split()

        Dim metersPerPixel = task.maxZ / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Dim sliceMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = -task.maxX * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
            If xCoordinate > task.topCameraPoint.X Then planeX = task.maxX * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)
            Dim depthMask As New cv.Mat
            minVal = planeX - thicknessMeters
            maxVal = planeX + thicknessMeters
            cv.Cv2.InRange(split(0).Clone, minVal, maxVal, depthMask)
            sliceMask.SetTo(255, depthMask)
            sliceMask.SetTo(0, task.noDepthMask)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = top2D.label2
    End Sub
End Class






Public Class Structured_MultiSlice : Inherits VBparent
    Public top2D As New Histogram_TopData
    Public side2D As New Histogram_SideData
    Dim struct As New Structured_SliceV
    Public sliceMask As cv.Mat
    Public split() As cv.Mat
    Public Sub New()
        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value
        Dim cushion = cushionSlider.Value

        top2D.Run(src)
        side2D.Run(src)

        Dim metersPerPixel = task.maxZ / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        split = side2D.gCloud.dst1.Split()

        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = -task.maxX * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
            If xCoordinate > task.topCameraPoint.X Then planeX = task.maxX * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)
            Dim depthMask As New cv.Mat
            minVal = planeX - thicknessMeters
            maxVal = planeX + thicknessMeters
            cv.Cv2.InRange(split(0).Clone, minVal, maxVal, depthMask)
            sliceMask = depthMask
            sliceMask.SetTo(0, task.noDepthMask)
            dst2.SetTo(255, sliceMask)
        Next

        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = -task.maxY * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
            If yCoordinate > task.sideCameraPoint.Y Then planeY = task.maxY * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)
            Dim depthMask As New cv.Mat
            minVal = planeY - thicknessMeters
            maxVal = planeY + thicknessMeters
            cv.Cv2.InRange(split(1).Clone, minVal, maxVal, depthMask)
            Dim tmp = depthMask
            cv.Cv2.BitwiseOr(tmp, sliceMask, sliceMask)
            dst2.SetTo(255, sliceMask)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, dst2)
    End Sub
End Class







Public Class Structured_MultiSliceLines : Inherits VBparent
    Dim multi As New Structured_MultiSlice
    Public ldetect As New Line_Basics
    Public Sub New()
        Dim lenSlider = findSlider("Line length threshold in pixels")
        lenSlider.Value = lenSlider.Maximum ' don't need the yellow line...
        task.desc = "Detect lines in the multiSlice output"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        multi.Run(src)
        cv.Cv2.BitwiseNot(multi.dst2, dst2)
        ldetect.Run(multi.dst2)
        dst1 = ldetect.dst1
    End Sub
End Class







Public Class Structured_MultiSlicePolygon : Inherits VBparent
    Dim multi As New Structured_MultiSlice
    Public Sub New()
        label1 = "Input to FindContours"
        label2 = "ApproxPolyDP 4-corner object from FindContours input"

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Max number of sides in the identified polygons", 3, 100, 4)
        End If
        task.desc = "Detect polygons in the multiSlice output"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static sidesSlider = findSlider("Max number of sides in the identified polygons")
        Dim maxSides = sidesSlider.Value

        multi.Run(src)
        cv.Cv2.BitwiseNot(multi.dst2, dst1)

        Dim rawContours = cv.Cv2.FindContoursAsArray(dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours(rawContours.Length - 1)() As cv.Point
        For j = 0 To rawContours.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(rawContours(j), 3, True)
        Next

        dst2.SetTo(0)
        For i = 0 To contours.Length - 1
            If contours(i).Length = 2 Then Continue For
            If contours(i).Length <= maxSides Then
                cv.Cv2.DrawContours(dst2, contours, i, New cv.Scalar(0, 255, 255), task.lineWidth + 1, task.lineType)
            End If
        Next
    End Sub
End Class






Public Class Structured_SliceXPlot : Inherits VBparent
    Dim multi As New Structured_MultiSlice
    Dim structD As New Structured_SliceV
    Public Sub New()
        structD.cushionSlider.Value = structD.cushionSlider.Maximum
        task.desc = "Find any plane around a peak value in the top-down histogram"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        structD.Run(src)
        dst2 = structD.dst2
        multi.Run(src)

        Dim col = If(task.mousePoint.X = 0, dst1.Width / 2, task.mousePoint.X)

        Dim cushion = structD.cushionSlider.Value
        Dim rect = New cv.Rect(col, 0, If(col + cushion >= dst2.Width, dst2.Width - col, cushion), dst2.Height - 1)
        Dim minLoc As cv.Point, maxLoc As cv.Point
        multi.top2D.histOutput(rect).MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        dst2.Circle(New cv.Point(col, dst2.Height - maxLoc.Y), task.dotSize + 6, cv.Scalar.Red, -1, task.lineType)
        dst2.Line(New cv.Point(0, dst2.Height - maxLoc.Y), New cv.Point(dst2.Width, dst2.Height - maxLoc.Y), cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Dim filterZ = maxLoc.Y / dst2.Height * task.maxZ

        Dim depthMask As New cv.Mat(multi.split(0).Size, cv.MatType.CV_8U)
        If filterZ > 0 Then cv.Cv2.InRange(multi.split(2), filterZ - 0.05, filterZ + 0.05, depthMask) ' a 10 cm buffer surrounding the z value
        If filterZ > 0 Then cv.Cv2.BitwiseAnd(multi.sliceMask, depthMask, depthMask)
        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, depthMask)

        label2 = "Peak histogram count (" + Format(maxVal, "#0") + ") at " + Format(filterZ, "#0.00") + " meters +-" + Format(5 / task.pixelsPerMeterTop, "#0.00") + " m"
        task.trueText("Use the mouse to move the slice.", 10, dst1.Height * 3 / 4, 3)
    End Sub
End Class







Public Class Structured_SliceYPlot : Inherits VBparent
    Dim multi As New Structured_MultiSlice
    Dim structD As New Structured_SliceH
    Public Sub New()
        structD.cushionSlider.Value = structD.cushionSlider.Maximum
        task.desc = "Find any plane around a peak value in the side view histogram"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 2
        structD.Run(src)
        dst2 = structD.dst2
        multi.Run(src)

        Dim row = If(task.mousePoint.Y = 0, dst1.Height / 2, task.mousePoint.Y)

        Dim cushion = structD.cushionSlider.Value
        Dim rect = New cv.Rect(0, row, dst2.Width - 1, If(row + cushion >= dst2.Height, dst2.Height - row, cushion))
        Dim minLoc As cv.Point, maxLoc As cv.Point
        multi.side2D.histOutput(rect).MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        If maxVal > 0 Then
            dst2.Circle(New cv.Point(maxLoc.X, row), task.dotSize + 6, cv.Scalar.Red, -1, task.lineType)
            dst2.Line(New cv.Point(maxLoc.X, 0), New cv.Point(maxLoc.X, dst2.Height), cv.Scalar.Yellow, task.lineWidth, task.lineType)
            Dim filterZ = maxLoc.X / dst2.Width * task.maxZ

            Dim depthMask As New cv.Mat(multi.split(1).Size, cv.MatType.CV_8U)
            cv.Cv2.InRange(multi.split(2), filterZ - 0.05, filterZ + 0.05, depthMask) ' a 10 cm buffer surrounding the z value

            dst1 = task.color.Clone
            dst1.SetTo(cv.Scalar.White, depthMask)
            Dim pixelsPerMeter = dst1.Width / task.maxZ
            label2 = "Peak histogram count (" + Format(maxVal, "#0") + ") at " + Format(filterZ, "#0.00") + " meters +-" + Format(5 / pixelsPerMeter, "#0.00") + " m"
            task.trueText("Use the mouse to move the slice.", 10, dst1.Height * 3 / 4, 3)
        End If
    End Sub
End Class







Public Class Structured_LinearizeFloor : Inherits VBparent
    Public floor As New Structured_Floor
    Dim kalman As New Kalman_VB_Basics
    Public imuPointCloud As cv.Mat
    Public sliceMask As cv.Mat
    Public floorYPlane As Single
    Public Sub New()
        If check.Setup(caller, 3) Then
            check.Box(0).Text = "Smooth in X-direction"
            check.Box(1).Text = "Smooth in Y-direction"
            check.Box(2).Text = "Smooth in Z-direction"
            check.Box(1).Checked = True
        End If
        task.desc = "Using the mask for the floor create a better representation of the floor plane"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static xCheck = findCheckBox("Smooth in X-direction")
        Static yCheck = findCheckBox("Smooth in Y-direction")
        Static zCheck = findCheckBox("Smooth in Z-direction")
        Dim minLoc As cv.Point, maxLoc As cv.Point
        Static imuPC As cv.Mat
        floor.Run(src)
        dst1 = floor.dst1
        dst2 = floor.dst2
        sliceMask = floor.structD.sliceMask

        Dim nonFloorMask As New cv.Mat
        cv.Cv2.BitwiseNot(sliceMask, nonFloorMask)
        imuPC = task.pointCloud.Clone
        imuPointCloud = imuPC.Clone
        imuPC.SetTo(0, nonFloorMask)

        If sliceMask.CountNonZero() > 0 Then
            Dim split = imuPC.Split()
            If xCheck.Checked Then
                split(0).MinMaxLoc(minVal, maxVal, minLoc, maxLoc, sliceMask)

                Dim firstCol As Integer, lastCol As Integer
                For firstCol = 0 To sliceMask.Width - 1
                    If sliceMask.Col(firstCol).CountNonZero() > 0 Then Exit For
                Next
                For lastCol = sliceMask.Width - 1 To 0 Step -1
                    If sliceMask.Col(lastCol).CountNonZero() Then Exit For
                Next

                Dim xIncr = (maxVal - minVal) / (lastCol - firstCol)
                For i = firstCol To lastCol
                    Dim maskCol = sliceMask.Col(i)
                    If maskCol.CountNonZero > 0 Then split(0).Col(i).SetTo(minVal + xIncr * i, maskCol)
                Next
            End If

            If yCheck.Checked Then
                split(1).MinMaxLoc(minVal, maxVal, minLoc, maxLoc, sliceMask)
                kalman.kInput = (minVal + maxVal) / 2
                kalman.Run(src)
                floorYPlane = kalman.kAverage
                split(1).SetTo(floorYPlane, sliceMask)
            End If

            If zCheck.Checked Then
                Dim firstRow As Integer, lastRow As Integer
                For firstRow = 0 To sliceMask.Height - 1
                    If sliceMask.Row(firstRow).CountNonZero() > 20 Then Exit For
                Next
                For lastRow = sliceMask.Height - 1 To 0 Step -1
                    If sliceMask.Row(lastRow).CountNonZero() > 20 Then Exit For
                Next

                If lastRow >= 0 And firstRow < sliceMask.Height Then
                    Dim meanMin = split(2).Row(lastRow).Mean(sliceMask.Row(lastRow))
                    Dim meanMax = split(2).Row(firstRow).Mean(sliceMask.Row(firstRow))
                    Dim zIncr = (meanMax.Item(0) - meanMin.Item(0)) / Math.Abs(lastRow - firstRow)
                    For i = firstRow To lastRow
                        Dim maskRow = sliceMask.Row(i)
                        Dim mean = split(2).Row(i).Mean(maskRow)
                        If maskRow.CountNonZero > 0 Then
                            split(2).Row(i).SetTo(mean.Item(0))
                            'Dim xy = New cv.Point3f(0, i, mean.Item(0))
                            'For xy.X = 0 To split(2).Width - 1
                            '    Dim xyz = getWorldCoordinates(xy)
                            '    imuPC.Set(Of cv.Point3f)(i, xy.X, xyz)
                            'Next
                        End If
                    Next
                    dst1.Line(New cv.Point(0, firstRow), New cv.Point(dst1.Width, firstRow), cv.Scalar.Yellow, task.lineWidth + 1)
                    dst1.Line(New cv.Point(0, lastRow), New cv.Point(dst1.Width, lastRow), cv.Scalar.Yellow, task.lineWidth + 1)
                End If
            End If

            cv.Cv2.Merge(split, imuPC)

            imuPC.CopyTo(imuPointCloud, sliceMask)
        End If
    End Sub
End Class






Public Class Structured_SliceH : Inherits VBparent
    Public tView As New TimeView_Basics
    Public cushionSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Public sliceOptions As New Structured_SliceOptions
    Public yPlaneOffset As Integer
    Public Sub New()
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        task.desc = "Find and isolate planes (floor and ceiling) in a side view histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        tView.Run(src)

        Dim depthShadow = task.noDepthMask
        Dim Split = tView.sideView.gCloud.dst1.Split()

        Dim yCoordinate = If(task.mousePoint.Y = 0, dst1.Height / 2, task.mousePoint.Y)

        Dim planeY = -task.maxY * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
        If yCoordinate > task.sideCameraPoint.Y Then planeY = task.maxY * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)

        Dim metersPerPixel = task.maxZ / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel
        dst1 = task.color.Clone
        Dim depthMask As New cv.Mat
        minVal = planeY - thicknessMeters
        maxVal = planeY + thicknessMeters
        cv.Cv2.InRange(Split(1).Clone, minVal, maxVal, depthMask)
        sliceMask = depthMask
        sliceMask.SetTo(0, task.noDepthMask)

        label1 = "At offset " + CStr(yCoordinate) + " y = " + Format((maxVal + minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(maxVal - minVal) * 100, "0.00") + " cm width"
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = tView.label2

        dst2 = tView.dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        yPlaneOffset = If(yCoordinate < dst2.Height - cushion, CInt(yCoordinate), dst2.Height - cushion - 1)
        dst2.Circle(New cv.Point(0, task.sideCameraPoint.Y), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        dst2.Line(New cv.Point(0, yPlaneOffset), New cv.Point(dst2.Width, yPlaneOffset), cv.Scalar.Yellow, cushion)
    End Sub
End Class







Public Class Structured_SliceV : Inherits VBparent
    Public tView As New TimeView_Basics
    Dim sideStruct As New Structured_SliceH
    Public cushionSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Public sliceOptions As New Structured_SliceOptions
    Public Sub New()
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        task.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.mousePoint = New cv.Point Then task.mousePoint = New cv.Point(dst1.Width / 2, dst1.Height)
        Dim xCoordinate = If(task.mousePoint.X = 0, dst1.Width / 2, task.mousePoint.X)
        tView.Run(src)

        Dim split = tView.topView.gCloud.dst1.Split()

        Dim planeX = -task.maxX * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
        If xCoordinate > task.topCameraPoint.X Then planeX = task.maxX * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)

        Dim metersPerPixel = task.maxZ / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel

        Dim depthMask As New cv.Mat
        minVal = planeX - thicknessMeters
        maxVal = planeX + thicknessMeters
        cv.Cv2.InRange(split(0).Clone, minVal, maxVal, depthMask)
        sliceMask = depthMask
        sliceMask.SetTo(0, task.noDepthMask)

        label1 = "At offset " + CStr(xCoordinate) + " x = " + Format((maxVal + minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(maxVal - minVal) * 100, "0.00") + " cm width"

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = tView.label2

        dst2 = tView.dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Circle(New cv.Point(task.topCameraPoint.X, dst2.Height), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        dst2.Line(New cv.Point(xCoordinate, 0), New cv.Point(xCoordinate, dst2.Height), cv.Scalar.Yellow, cushion)
    End Sub
End Class








Public Class Structured_SliceVStable : Inherits VBparent
    Public top2D As New Histogram_TopData
    Dim structD As New Structured_SliceV
    Public cushionSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Public Sub New()
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        task.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim xCoordinate = If(task.mousePoint.X = 0, dst1.Width / 2, task.mousePoint.X)
        top2D.Run(src)
        dst2 = top2D.dst1
        Dim split = top2D.gCloud.dst1.Split()

        Dim planeX = -task.maxX * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
        If xCoordinate > task.topCameraPoint.X Then planeX = task.maxX * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)

        Dim metersPerPixel = task.maxZ / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel

        Dim depthMask As New cv.Mat
        minVal = planeX - thicknessMeters
        maxVal = planeX + thicknessMeters
        cv.Cv2.InRange(split(0).Clone, minVal, maxVal, depthMask)
        sliceMask = depthMask
        sliceMask.SetTo(0, task.noDepthMask)

        label1 = "At offset " + CStr(xCoordinate) + " x = " + Format((maxVal + minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(maxVal - minVal) * 100, "0.00") + " cm width"

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = top2D.label2
    End Sub
End Class








Public Class Structured_MouseSlice : Inherits VBparent
    Dim vSlice As New Structured_SliceV
    Dim line As New Line_Basics
    Public Sub New()
        label1 = "Center Slice in yellow"
        label2 = "White = SliceV output, Red Dot is avgPt"
        task.desc = "Find the vertical center line with accurate depth data.."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.mousePoint = New cv.Point Then task.mousePoint = New cv.Point(dst1.Width / 2, dst1.Height)
        vSlice.Run(src)
        dst1 = task.color

        line.Run(vSlice.sliceMask)
        Dim tops As New List(Of Integer)
        Dim bots As New List(Of Integer)
        Dim topsList As New List(Of cv.Point)
        Dim botsList As New List(Of cv.Point)
        If line.sortlines.Count > 0 Then
            dst2 = line.dst1
            For i = 0 To line.sortlines.Count - 1
                Dim p1 = line.pt1List(i)
                Dim p2 = line.pt2List(i)
                dst2.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth + 3, task.lineType)
                tops.Add(If(p1.Y < p2.Y, p1.Y, p2.Y))
                bots.Add(If(p1.Y > p2.Y, p1.Y, p2.Y))
                topsList.Add(p1)
                botsList.Add(p2)
            Next

            Dim topPt = topsList(tops.IndexOf(tops.Min))
            Dim botPt = botsList(bots.IndexOf(bots.Max))
            dst2.Circle(New cv.Point2f((topPt.X + botPt.X) / 2, (topPt.Y + botPt.Y) / 2), task.dotSize + 5, cv.Scalar.Red, -1, task.lineType)
            dst2.Line(topPt, botPt, cv.Scalar.Red, task.lineWidth, task.lineType)
            dst1.Line(topPt, botPt, cv.Scalar.Yellow, task.lineWidth + 2, task.lineType)
        End If
    End Sub
End Class








Public Class Structured_CloudFail : Inherits VBparent
    Dim mmPixel As Pixel_Measure
    Public Sub New()
        mmPixel = New Pixel_Measure
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Lines in X-Direction", 0, 200, 50)
            sliders.setupTrackBar(1, "Lines in Y-Direction", 0, 200, 50)
            sliders.setupTrackBar(2, "Continuity threshold in mm", 0, 100, 10)
        End If

        If check.Setup(caller, 3) Then
            check.Box(0).Text = "Impose constraints on X"
            check.Box(1).Text = "Impose constraints on Y"
            check.Box(2).Text = "Impose constraints on neither"
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If

        task.desc = "Attempt to impose a structure on the point cloud data."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static xLineSlider = findSlider("Lines in X-Direction")
        Static yLineSlider = findSlider("Lines in Y-Direction")
        Static thresholdSlider = findSlider("Continuity threshold in mm")

        Static xCheck = findCheckBox("Impose constraints on X")
        Static yCheck = findCheckBox("Impose constraints on Y")
        Static noCheck = findCheckBox("Impose constraints on neither")

        Dim xLines = xLineSlider.value
        Dim yLines = yLineSlider.value
        Dim threshold = thresholdSlider.value

        Dim xconstraint = xCheck.checked
        Dim yconstraint = yCheck.checked
        Dim noconstraint = noCheck.checked
        If noconstraint Then
            xconstraint = False
            yconstraint = False
        End If

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        Dim stepX = dst1.Width / xLines
        Dim stepY = dst1.Height / yLines
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC3, 0)
        Dim midX = dst1.Width / 2
        Dim midY = dst1.Height / 2
        Dim halfStepX = stepX / 2
        Dim halfStepy = stepY / 2
        For y = 1 To yLines - 2
            For x = 1 To xLines - 2
                Dim pt1 = New cv.Point2f(x * stepX, y * stepY)
                Dim pt2 = New cv.Point2f((x + 1) * stepX, y * stepY)
                Dim d1 = task.depth32f.Get(Of Single)(pt1.Y, pt1.X)
                Dim d2 = task.depth32f.Get(Of Single)(pt2.Y, pt2.X)
                If stepX * threshold > Math.Abs(d1 - d2) And d1 > 0 And d2 > 0 Then
                    Dim p = task.pointCloud.Get(Of cv.Vec3f)(pt1.Y, pt1.X)
                    Dim mmPP = mmPixel.Compute(d1)
                    If noconstraint = False Then
                        If xconstraint Then
                            p.Item0 = (pt1.X - midX) * mmPP / 1000
                            If pt1.X = midX Then p.Item0 = mmPP / 1000
                        End If
                        If yconstraint Then
                            p.Item1 = (pt1.Y - midY) * mmPP / 1000
                            If pt1.Y = midY Then p.Item1 = mmPP / 1000
                        End If
                    End If
                    Dim r = New cv.Rect(pt1.X - halfStepX, pt1.Y - halfStepy, stepX, stepY)
                    Dim meanVal = cv.Cv2.Mean(task.depth32f(r), task.depthMask(r))
                    p.Item2 = (d1 + d2) / 2000 ' meanVal.Item(0) / 1000
                    dst2.Set(Of cv.Vec3f)(y, x, p)
                End If
            Next
        Next
        dst1 = dst2(New cv.Rect(0, 0, xLines, yLines)).Resize(dst1.Size, 0, 0, cv.InterpolationFlags.Nearest)
    End Sub
End Class








Public Class Structured_Cloud : Inherits VBparent
    Public data As New cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Number of slices", 0, 200, 100)
            sliders.setupTrackBar(1, "Slice index X", 1, 200, 50)
            sliders.setupTrackBar(2, "Slice index Y", 1, 200, 50)
        End If

        task.desc = "Attempt to impose a linear structure on the pointcloud."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static sliceSlider = findSlider("Number of slices")
        Dim xLines = sliceSlider.value
        Dim yLines = CInt(xLines * dst1.Height / dst1.Width)

        Dim stepX = dst2.Width / xLines
        Dim stepY = dst2.Height / yLines
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC3, 0)
        For y = 0 To yLines - 1
            For x = 0 To xLines - 1
                Dim pt1 = New cv.Point(CInt(x * stepX), CInt(y * stepY))
                Dim pt2 = New cv.Point(CInt((x + 1) * stepX), CInt(y * stepY))
                Dim p1 = task.pointCloud.Get(Of cv.Vec3f)(pt1.Y, pt1.X)
                Dim p2 = task.pointCloud.Get(Of cv.Vec3f)(pt2.Y, pt2.X)
                If p1.Item2 > 0 And p2.Item2 > 0 Then
                    dst2.Set(Of cv.Vec3f)(y, x, p1)
                End If
            Next
        Next
        Dim rect = New cv.Rect(0, 0, xLines, yLines)
        data = dst2(rect).Clone
        label1 = "Structured_Cloud with " + CStr(yLines) + " rows " + CStr(xLines) + " columns"
        dst1 = dst2(rect).Resize(dst1.Size, 0, 0, cv.InterpolationFlags.Nearest)
    End Sub
End Class








Public Class Structured_Crosshairs : Inherits VBparent
    Dim sCloud As New Structured_Cloud
    Public Sub New()
        task.desc = "Connect vertical and horizontal dots that are in the same column and row."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static sliceSlider = findSlider("Number of slices")
        Static xSlider = findSlider("Slice index X")
        Static ySlider = findSlider("Slice index Y")
        Dim xLines = sliceSlider.value
        Dim yLines = CInt(xLines * dst1.Width / dst1.Height)
        Dim indexX = xSlider.value
        Dim indexY = ySlider.value
        If indexX > xLines Then indexX = xLines - 1
        If indexY > yLines Then indexY = yLines - 1

        sCloud.Run(src)
        Dim data = sCloud.data
        Dim split = cv.Cv2.Split(data)
        Dim minX As Double, maxX As Double
        cv.Cv2.MinMaxLoc(split(0), minX, maxX)

        Dim minY As Double, maxY As Double
        cv.Cv2.MinMaxLoc(split(1), minY, maxY)

        ' using the above min/max values to establish a rigid grid to reduce shakiness in the output.
        minX = -2.5
        maxX = 2.0
        minY = -2.0
        maxY = 1.25

        dst1.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        Dim pointX As New cv.Mat(data.Size, cv.MatType.CV_32S, 0)
        Dim pointY As New cv.Mat(data.Size, cv.MatType.CV_32S, 0)
        Dim yy As Integer, xx As Integer
        For y = 1 To data.Height - 1
            For x = 1 To data.Width - 1
                Dim p = data.Get(Of cv.Vec3f)(y, x)
                If p.Item2 > 0 Then
                    xx = dst1.Width * (maxX - p.Item0) / (maxX - minX)
                    yy = dst1.Height * (maxY - p.Item1) / (maxY - minY)
                    If xx < 0 Then xx = 0
                    If yy < 0 Then yy = 0
                    If xx >= dst1.Width Then xx = dst1.Width - 1
                    If yy >= dst1.Height Then yy = dst1.Height - 1
                    yy = dst1.Height - yy - 1
                    xx = dst1.Width - xx - 1
                    dst1.Set(Of cv.Vec3b)(yy, xx, white)

                    pointX.Set(Of Integer)(y, x, xx)
                    pointY.Set(Of Integer)(y, x, yy)
                    If x = indexX Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y - 1, x), pointY.Get(Of Integer)(y - 1, x))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst1.Line(p1, p2, cv.Scalar.White, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                    If y = indexY Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y, x - 1), pointY.Get(Of Integer)(y, x - 1))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst1.Line(p1, p2, cv.Scalar.White, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                End If
            Next
        Next
    End Sub
End Class
