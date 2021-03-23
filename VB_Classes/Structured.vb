Imports cv = OpenCvSharp
Public Class Structured_Floor
    Inherits VBparent
    Public structD As Structured_SliceH
    Dim kalman As Kalman_VB_Basics
    Public floorYplane As Single
    Public Sub New()
        initParent()
        kalman = New Kalman_VB_Basics()

        structD = New Structured_SliceH()
        task.thresholdSlider.Value = 10 ' some cameras can show data below ground level...
        structD.cushionSlider.Value = 5 ' floor runs can use a thinner slice that ceilings...

        task.desc = "Find the floor plane"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        structD.Run()

        Dim yCoordinate = dst2.Height
        Dim lastSum = dst2.Row(dst2.Height - 1).Sum()
        For yCoordinate = dst2.Height - 1 To 0 Step -1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 1000 Then Exit For
            lastSum = nextSum
        Next

        kalman.kInput = yCoordinate
        kalman.Run()

        ' it settles down quicker...
        If ocvb.frameCount > 30 Then yCoordinate = kalman.kAverage

        floorYplane = structD.side2D.meterMax * (yCoordinate - ocvb.sideCameraPoint.Y) / (dst2.Height - ocvb.sideCameraPoint.Y)

        structD.offsetSlider.Value = If(yCoordinate >= 0, yCoordinate, 0)

        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class








Public Class Structured_Ceiling
    Inherits VBparent
    Public structD As Structured_SliceH
    Dim kalman As Kalman_Basics
    Public Sub New()
        initParent()
        kalman = New Kalman_Basics()
        ReDim kalman.kInput(0)

        structD = New Structured_SliceH()
        structD.cushionSlider.Value = 10
        task.desc = "Find the ceiling plane"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        structD.Run()

        Dim yCoordinate As Integer
        Dim lastSum = dst2.Row(yCoordinate).Sum()
        For yCoordinate = 1 To dst2.Height - 1
            Dim nextSum = dst2.Row(yCoordinate).Sum()
            If nextSum.Item(0) - lastSum.Item(0) > 1000 Then Exit For
            lastSum = nextSum
        Next

        kalman.kInput(0) = yCoordinate
        kalman.Run()
        structD.offsetSlider.Value = If(kalman.kOutput(0) >= 0, kalman.kOutput(0), 0)

        dst1 = structD.dst1
        dst2 = structD.dst2
    End Sub
End Class






Public Class Structured_MultiSliceH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Public structD As Structured_SliceH
    Public sliceMask As cv.Mat
    Dim inrange As Depth_InRange
    Public Sub New()
        initParent()
        side2D = New Histogram_SideData()
        inrange = New Depth_InRange()
        structD = New Structured_SliceH()

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run()
        dst2 = side2D.dst2
        Dim Split = side2D.gCloud.dst1.Split()

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        sliceMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (ocvb.sideCameraPoint.Y - yCoordinate) / ocvb.sideCameraPoint.Y
            If yCoordinate > ocvb.sideCameraPoint.Y Then planeY = side2D.meterMax * (yCoordinate - ocvb.sideCameraPoint.Y) / (dst2.Height - ocvb.sideCameraPoint.Y)
            inrange.minVal = planeY - thicknessMeters
            inrange.maxVal = planeY + thicknessMeters
            inrange.src = Split(1).Clone
            inrange.Run()
            sliceMask.SetTo(255, inrange.depthMask)
            sliceMask.SetTo(0, task.inrange.noDepthMask)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = side2D.label2
    End Sub
End Class






Public Class Structured_MultiSliceV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public structD As Structured_SliceH
    Dim inrange As Depth_InRange
    Public Sub New()
        initParent()

        top2D = New Histogram_TopData()
        inrange = New Depth_InRange()
        structD = New Structured_SliceH()

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        top2D.Run()
        dst2 = top2D.dst2

        Dim split = top2D.gCloud.dst1.Split()

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        Dim sliceMask = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = top2D.meterMin * (ocvb.topCameraPoint.X - xCoordinate) / ocvb.topCameraPoint.X
            If xCoordinate > ocvb.topCameraPoint.X Then planeX = top2D.meterMax * (xCoordinate - ocvb.topCameraPoint.X) / (dst2.Width - ocvb.topCameraPoint.X)
            inrange.minVal = planeX - thicknessMeters
            inrange.maxVal = planeX + thicknessMeters
            inrange.src = split(0).Clone
            inrange.Run()
            sliceMask.SetTo(255, inrange.depthMask)
            sliceMask.SetTo(0, task.inrange.noDepthMask)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = top2D.label2
    End Sub
End Class






Public Class Structured_MultiSlice
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public side2D As Histogram_SideData
    Dim struct As Structured_SliceV
    Public inrange As Depth_InRange
    Public sliceMask As cv.Mat
    Public split() As cv.Mat
    Public Sub New()
        initParent()

        side2D = New Histogram_SideData()
        top2D = New Histogram_TopData()
        inrange = New Depth_InRange()
        struct = New Structured_SliceV()

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        top2D.Run()
        side2D.Run()

        Static cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        Dim cushion = cushionSlider.Value

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim thicknessMeters = cushion * metersPerPixel

        Static stepSlider = findSlider("Slice step size in pixels (multi-slice option only)")
        Dim stepsize = stepSlider.value

        split = side2D.gCloud.dst1.Split()

        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepsize
            Dim planeX = top2D.meterMin * (ocvb.topCameraPoint.X - xCoordinate) / ocvb.topCameraPoint.X
            If xCoordinate > ocvb.topCameraPoint.X Then planeX = top2D.meterMax * (xCoordinate - ocvb.topCameraPoint.X) / (dst2.Width - ocvb.topCameraPoint.X)
            inrange.minVal = planeX - thicknessMeters
            inrange.maxVal = planeX + thicknessMeters
            inrange.src = split(0).Clone
            inrange.Run()
            sliceMask = inrange.depthMask
            sliceMask.SetTo(0, task.inrange.noDepthMask)
            dst2.SetTo(255, sliceMask)
        Next

        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (ocvb.sideCameraPoint.Y - yCoordinate) / ocvb.sideCameraPoint.Y
            If yCoordinate > ocvb.sideCameraPoint.Y Then planeY = side2D.meterMax * (yCoordinate - ocvb.sideCameraPoint.Y) / (dst2.Height - ocvb.sideCameraPoint.Y)
            inrange.minVal = planeY - thicknessMeters
            inrange.maxVal = planeY + thicknessMeters
            inrange.src = split(1).Clone
            inrange.Run()
            Dim tmp = inrange.depthMask
            cv.Cv2.BitwiseOr(tmp, sliceMask, sliceMask)
            dst2.SetTo(255, sliceMask)
        Next

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, dst2)
    End Sub
End Class







Public Class Structured_MultiSliceLines
    Inherits VBparent
    Dim multi As Structured_MultiSlice
    Public ldetect As LineDetector_Basics
    Public Sub New()
        initParent()
        ldetect = New LineDetector_Basics()
        Dim lenSlider = findSlider("Line length threshold in pixels")
        lenSlider.Value = lenSlider.Maximum ' don't need the yellow line...
        multi = New Structured_MultiSlice()
        task.desc = "Detect lines in the multiSlice output"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        multi.Run()
        cv.Cv2.BitwiseNot(multi.dst2, dst2)
        ldetect.src = multi.dst2
        ldetect.Run()
        dst1 = ldetect.dst1
    End Sub
End Class







Public Class Structured_MultiSlicePolygon
    Inherits VBparent
    Dim multi As Structured_MultiSlice
    Public Sub New()
        initParent()
        multi = New Structured_MultiSlice()
        label1 = "Input to FindContours"
        label2 = "ApproxPolyDP 4-corner object from FindContours input"

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Max number of sides in the identified polygons", 3, 100, 4)
        End If
        task.desc = "Detect polygons in the multiSlice output"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        multi.Run()
        cv.Cv2.BitwiseNot(multi.dst2, dst1)

        Dim rawContours = cv.Cv2.FindContoursAsArray(dst1, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours(rawContours.Length - 1)() As cv.Point
        For j = 0 To rawContours.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(rawContours(j), 3, True)
        Next

        dst2.SetTo(0)
        Dim sidesSlider = findSlider("Max number of sides in the identified polygons")
        Dim maxSides = sidesSlider.Value
        For i = 0 To contours.Length - 1
            If contours(i).Length = 2 Then Continue For
            If contours(i).Length <= maxSides Then
                cv.Cv2.DrawContours(dst2, contours, i, New cv.Scalar(0, 255, 255), 2, cv.LineTypes.AntiAlias)
            End If
        Next
    End Sub
End Class






Public Class Structured_SliceXPlot
    Inherits VBparent
    Dim multi As Structured_MultiSlice
    Dim structD As Structured_SliceV
    Dim cushionSlider As Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        multi = New Structured_MultiSlice()
        structD = New Structured_SliceV()
        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        cushionSlider.Value = 25
        task.desc = "Find any plane around a peak value in the top-down histogram"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        structD.Run()
        dst2 = structD.dst2
        multi.Run()

        Static offsetSlider = findSlider("Offset for the vertical slice")
        Dim col = CInt(offsetSlider.value)

        Dim cushion = cushionSlider.Value
        Dim rect = New cv.Rect(col, 0, If(col + cushion >= dst2.Width, dst2.Width - col, cushion), dst2.Height - 1)
        Dim minVal As Double, maxVal As Double
        Dim minLoc As cv.Point, maxLoc As cv.Point
        multi.top2D.histOutput(rect).MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        dst2.Circle(New cv.Point(col, dst2.Height - maxLoc.Y), 10, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        Dim filterZ = maxLoc.Y / dst2.Height * ocvb.maxZ

        Dim maskZplane As New cv.Mat(multi.split(0).Size, cv.MatType.CV_8U)
        If filterZ > 0 Then
            multi.inrange.minVal = filterZ - 0.05 ' a 10 cm buffer surrounding the z value
            multi.inrange.maxVal = filterZ + 0.05
            multi.inrange.src = multi.split(2)
            multi.inrange.Run()
            maskZplane = multi.inrange.depthMask
        End If

        If filterZ > 0 Then cv.Cv2.BitwiseAnd(multi.sliceMask, maskZplane, maskZplane)

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, maskZplane)

        label2 = "Peak histogram count (" + Format(maxVal, "#0") + ") at " + Format(filterZ, "#0.00") + " meters +-" + Format(10 / ocvb.pixelsPerMeter, "#0.00") + " m"
    End Sub
End Class







Public Class Structured_LinearizeFloor
    Inherits VBparent
    Public floor As Structured_Floor
    Dim kalman As Kalman_VB_Basics
    Public imuPointCloud As cv.Mat
    Public sliceMask As cv.Mat
    Public floorYPlane As Single
    Public Sub New()
        initParent()
        kalman = New Kalman_VB_Basics()
        floor = New Structured_Floor()

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
            check.Box(0).Text = "Smooth in X-direction"
            check.Box(1).Text = "Smooth in Y-direction"
            check.Box(2).Text = "Smooth in Z-direction"
            check.Box(1).Checked = True
        End If
        task.desc = "Using the mask for the floor create a better representation of the floor plane"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim minVal As Double, maxVal As Double
        Dim minLoc As cv.Point, maxLoc As cv.Point
        Static imuPC As cv.Mat
        floor.Run()
        dst1 = floor.dst1
        dst2 = floor.dst2
        sliceMask = floor.structD.sliceMask
        If sliceMask.CountNonZero() > 0 Then
            Dim nonFloorMask As New cv.Mat
            cv.Cv2.BitwiseNot(sliceMask, nonFloorMask)
            imuPC = task.pointCloud.Clone
            imuPointCloud = imuPC.Clone
            imuPC.SetTo(0, nonFloorMask)

            Dim split = imuPC.Split()
            Static xCheck = findCheckBox("Smooth in X-direction")
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

            Static yCheck = findCheckBox("Smooth in Y-direction")
            If yCheck.Checked Then
                split(1).MinMaxLoc(minVal, maxVal, minLoc, maxLoc, sliceMask)
                kalman.kInput = (minVal + maxVal) / 2
                kalman.Run()
                floorYPlane = kalman.kAverage
                split(1).SetTo(floorYPlane, sliceMask)
            End If

            Static zCheck = findCheckBox("Smooth in Z-direction")
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
                    dst1.Line(New cv.Point(0, firstRow), New cv.Point(dst1.Width, firstRow), cv.Scalar.Yellow, 2)
                    dst1.Line(New cv.Point(0, lastRow), New cv.Point(dst1.Width, lastRow), cv.Scalar.Yellow, 2)
                End If
            End If

            cv.Cv2.Merge(split, imuPC)

            imuPC.CopyTo(imuPointCloud, sliceMask)
        End If
    End Sub
End Class







Public Class Structured_SliceH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Dim inrange As Depth_InRange
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Public yPlaneOffset As Integer
    Public Sub New()
        initParent()
        side2D = New Histogram_SideData()
        inrange = New Depth_InRange()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 100, 1)
            sliders.setupTrackBar(1, "Offset for the horizontal slice", 0, src.Height - 1, src.Height / 2)
            sliders.setupTrackBar(2, "Offset for the vertical slice", 0, src.Width - 1, src.Width / 2)
            sliders.setupTrackBar(3, "Slice step size in pixels (multi-slice option only)", 1, 100, 20)
        End If

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the horizontal slice")

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        task.desc = "Find and isolate planes (floor and ceiling) in a side view histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        side2D.Run()

        Dim depthShadow = task.inrange.noDepthMask
        Dim Split = side2D.gCloud.dst1.Split()

        Dim yCoordinate = CInt(offsetSlider.Value)

        Dim planeY = side2D.meterMin * (ocvb.sideCameraPoint.Y - yCoordinate) / ocvb.sideCameraPoint.Y
        If yCoordinate > ocvb.sideCameraPoint.Y Then planeY = side2D.meterMax * (yCoordinate - ocvb.sideCameraPoint.Y) / (dst2.Height - ocvb.sideCameraPoint.Y)

        Dim metersPerPixel = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel
        dst1 = task.color.Clone
        inrange.minVal = planeY - thicknessMeters
        inrange.maxVal = planeY + thicknessMeters
        inrange.src = Split(1).Clone
        inrange.Run()
        sliceMask = inrange.depthMask
        sliceMask.SetTo(0, task.inrange.noDepthMask)

        label1 = "At offset " + CStr(yCoordinate) + " y = " + Format((inrange.maxVal + inrange.minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(inrange.maxVal - inrange.minVal) * 100, "0.00") + " cm width"
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = side2D.label2

        dst2 = side2D.dst1.ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        yPlaneOffset = If(offsetSlider.Value < dst2.Height - cushion, CInt(offsetSlider.Value), dst2.Height - cushion - 1)
        dst2.Circle(New cv.Point(0, ocvb.sideCameraPoint.Y), ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        dst2.Line(New cv.Point(0, yPlaneOffset), New cv.Point(dst2.Width, yPlaneOffset), cv.Scalar.Yellow, cushion)
    End Sub
End Class







Public Class Structured_SliceV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public inrange As Depth_InRange
    Dim sideStruct As Structured_SliceH
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Public Sub New()
        initParent()
        top2D = New Histogram_TopData()
        inrange = New Depth_InRange()
        sideStruct = New Structured_SliceH()

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the vertical slice")
        offsetSlider.Maximum = src.Width - 1
        offsetSlider.Value = src.Width / 2

        task.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim xCoordinate = offsetSlider.Value
        top2D.Run()

        Dim split = top2D.gCloud.dst1.Split()

        Dim planeX = top2D.meterMin * (ocvb.topCameraPoint.X - xCoordinate) / ocvb.topCameraPoint.X
        If xCoordinate > ocvb.topCameraPoint.X Then planeX = top2D.meterMax * (xCoordinate - ocvb.topCameraPoint.X) / (dst2.Width - ocvb.topCameraPoint.X)

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel

        inrange.minVal = planeX - thicknessMeters
        inrange.maxVal = planeX + thicknessMeters
        inrange.src = split(0).Clone
        inrange.Run()
        sliceMask = inrange.depthMask
        sliceMask.SetTo(0, task.inrange.noDepthMask)

        label1 = "At offset " + CStr(xCoordinate) + " x = " + Format((inrange.maxVal + inrange.minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(inrange.maxVal - inrange.minVal) * 100, "0.00") + " cm width"

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = top2D.label2

        dst2 = top2D.dst1.Normalize(0, 255, cv.NormTypes.MinMax)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Circle(New cv.Point(ocvb.topCameraPoint.X, dst2.Height), ocvb.dotSize, cv.Scalar.Yellow, -1, cv.LineTypes.AntiAlias)
        Dim offset = CInt(offsetSlider.Value)
        dst2.Line(New cv.Point(offset, 0), New cv.Point(offset, dst2.Height), cv.Scalar.Yellow, cushion)
    End Sub
End Class














Public Class Structured_SliceVStable
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public inrange As Depth_InRange
    Dim sideStruct As Structured_SliceH
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Public Sub New()
        initParent()
        top2D = New Histogram_TopData()
        inrange = New Depth_InRange()
        sideStruct = New Structured_SliceH()

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Offset for the vertical slice")
        offsetSlider.Value = src.Width / 2

        task.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Dim xCoordinate = offsetSlider.Value
        top2D.Run()
        dst2 = top2D.dst1
        Dim split = top2D.gCloud.dst1.Split()

        Dim planeX = top2D.meterMin * (ocvb.topCameraPoint.X - xCoordinate) / ocvb.topCameraPoint.X
        If xCoordinate > ocvb.topCameraPoint.X Then planeX = top2D.meterMax * (xCoordinate - ocvb.topCameraPoint.X) / (dst2.Width - ocvb.topCameraPoint.X)

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
        Dim cushion = cushionSlider.Value
        Dim thicknessMeters = cushion * metersPerPixel

        inrange.minVal = planeX - thicknessMeters
        inrange.maxVal = planeX + thicknessMeters
        inrange.src = split(0).Clone
        inrange.Run()
        sliceMask = inrange.depthMask
        sliceMask.SetTo(0, task.inrange.noDepthMask)

        label1 = "At offset " + CStr(xCoordinate) + " x = " + Format((inrange.maxVal + inrange.minVal) / 2, "#0.00") + " with " +
                 Format(Math.Abs(inrange.maxVal - inrange.minVal) * 100, "0.00") + " cm width"

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, sliceMask)
        label2 = top2D.label2
    End Sub
End Class








Public Class Structured_CenterSlice
    Inherits VBparent
    Dim vSlice As Structured_SliceV
    Dim line As LineDetector_Basics
    Public topPt As cv.Point2f, botPt As cv.Point2f
    Public slope As Single
    Public avgPt As cv.Point2f
    Public b As Integer
    Public Sub New()
        initParent()
        vSlice = New Structured_SliceV
        line = New LineDetector_Basics
        label1 = "Center Slice in yellow"
        label2 = "White = SliceV output, Red Dot is avgPt"
        task.desc = "Find the vertical center line with accurate depth data.."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me

        vSlice.Run()
        dst1 = task.color

        line.src = vSlice.sliceMask
        line.Run()
        dst2 = line.dst1

        If line.sortlines.Count > 0 Then
            Dim count As Integer
            Dim minY = Single.MaxValue, maxY = Single.MinValue
            avgPt = New cv.Point2f
            For Each nl In line.sortlines
                Dim p1 = New cv.Point2f(nl.Value.Item0, nl.Value.Item1)
                Dim p2 = New cv.Point2f(nl.Value.Item2, nl.Value.Item3)
                If Math.Abs(p2.X - p1.X) > 1 Then
                    avgPt += p1
                    avgPt += p2
                    Dim nextSlope = (p2.Y - p1.Y) / (p2.X - p1.X)
                    slope = If(slope = 0, nextSlope, (nextSlope + slope) / 2)
                    count += 2
                End If
                If p1.Y < minY Then
                    topPt = p1
                    minY = p1.Y
                End If
                If p1.Y > maxY Then
                    botPt = p1
                    maxY = p1.Y
                End If
                If p2.Y < minY Then
                    topPt = p2
                    minY = p2.Y
                End If
                If p2.Y > maxY Then
                    botPt = p2
                    maxY = p2.Y
                End If
            Next

            If count > 0 Then
                If slope = 0 And topPt.X <> botPt.X Then slope = (topPt.Y - botPt.Y) / (topPt.X - botPt.X)
                avgPt = New cv.Point2f(avgPt.X / count, avgPt.Y / count)
                b = avgPt.Y - slope * avgPt.X ' y = slope * x + b, b = y - slope * x
                topPt = New cv.Point2f(-b / slope, 0)  ' y = 0, 0 = slope * x + b, x = -b / slope
                botPt = New cv.Point2f((dst1.Height - b) / slope, dst1.Height)
            End If
        End If

        If Math.Abs(topPt.X - botPt.X) < 1 Or topPt.Y <> 0 Or botPt.Y <> dst1.Height Then
            topPt = New cv.Point2f(topPt.X, 0)
            botPt = New cv.Point2f(topPt.X, dst1.Height)
        End If
        dst2.Circle(avgPt, ocvb.dotSize, cv.Scalar.Red, -1, cv.LineTypes.AntiAlias)
        dst2.Line(topPt, botPt, cv.Scalar.Red, 1, cv.LineTypes.AntiAlias)
        dst1.Line(topPt, botPt, cv.Scalar.Yellow, 1, cv.LineTypes.AntiAlias)
    End Sub
End Class







Public Class Structured_CloudOld
    Inherits VBparent
    Dim line As Structured_CenterSlice
    Public Sub New()
        initParent()
        line = New Structured_CenterSlice

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Lines in X-Direction", 0, 100, 30)
            sliders.setupTrackBar(1, "Lines in Y-Direction", 0, 100, 30)
        End If

        dst2 = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)

        task.desc = "Attempt to impose a structure on the point cloud data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static xLineSlider = findSlider("Lines in X-Direction")
        Static yLineSlider = findSlider("Lines in Y-Direction")
        Dim xLines = xLineSlider.value
        Dim yLines = yLineSlider.value

        line.Run()
        dst1 = src

        Dim topPt = line.topPt
        Dim botPt = line.botPt
        dst2.SetTo(0)
        dst2.Line(topPt, botPt, 255, 1, cv.LineTypes.AntiAlias)

        Dim stepX = -(topPt.X - botPt.X) / yLines ' negative because the y-axis is increasing down.
        Dim stepY = dst1.Height / yLines
        If stepX < 0.5 Then stepX = 0

        Dim slope = 1 / line.slope ' perpendiculars have inverse slope
        If topPt.X = botPt.X Then slope = 0
        For i = 0 To yLines - 1
            Dim x = topPt.X + stepX * i
            Dim pt = New cv.Point2f(x, line.slope * x + line.b)
            If stepX = 0 Then pt = New cv.Point2f(x, stepY * i)
            Dim b = pt.Y + slope * pt.X
            Dim pt1 = New cv.Point2f(dst1.Width, -slope * dst1.Width + b)
            Dim pt2 = New cv.Point2f(0, b)
            dst2.Line(pt1, pt2, 255, 1, cv.LineTypes.AntiAlias)
        Next

    End Sub
End Class








Public Class Structured_Cloud
    Inherits VBparent
    Dim mmPixel As Pixel_Measure
    Public Sub New()
        initParent()
        mmPixel = New Pixel_Measure
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Lines in X-Direction", 0, 200, 100)
            sliders.setupTrackBar(1, "Lines in Y-Direction", 0, 200, 100)
            sliders.setupTrackBar(2, "Continuity threshold in mm", 0, 100, 10)
        End If

        dst1 = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)

        task.desc = "Attempt to impose a structure on the point cloud data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then ocvb.intermediateObject = Me
        Static xLineSlider = findSlider("Lines in X-Direction")
        Static yLineSlider = findSlider("Lines in Y-Direction")
        Static thresholdSlider = findSlider("Continuity threshold in mm")
        Dim xLines = xLineSlider.value
        Dim yLines = yLineSlider.value
        Dim threshold = thresholdSlider.value

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.depth32f

        Dim topPt = New cv.Point2f(dst1.Width / 2, 0)
        Dim botPt = New cv.Point2f(dst1.Width / 2, dst1.Height)
        dst1 = task.RGBDepth

        Dim stepX = dst1.Width / xLines
        Dim stepY = dst1.Height / yLines
        dst2 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC3, 0)
        For y = 0 To yLines - 1
            For x = 1 To xLines - 1
                Dim pt1 = New cv.Point2f((x - 1) * stepX, y * stepY)
                Dim pt2 = New cv.Point2f(x * stepX, y * stepY)
                Dim d1 = task.depth32f.Get(Of Single)(pt1.Y, pt1.X)
                Dim d2 = task.depth32f.Get(Of Single)(pt2.Y, pt2.X)
                If stepX * threshold > Math.Abs(d1 - d2) And d1 > 0 And d2 > 0 Then
                    Dim p = task.pointCloud.Get(Of cv.Vec3f)(pt1.Y, pt1.X)
                    p.Item2 = (d1 + d2) / 2000
                    dst1.Line(pt1, pt2, cv.Scalar.White, 1)
                    dst2.Set(Of cv.Vec3f)(y, x, p)
                End If
            Next
        Next
        dst2 = dst2(New cv.Rect(0, 0, xLines, yLines)).Resize(dst1.Size, 0, 0, cv.InterpolationFlags.Nearest)
    End Sub
End Class
