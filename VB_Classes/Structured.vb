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
        structD.cushionSlider.Value = 5 ' floor runs can use a thinner slice that ceilings...

        task.desc = "Find the floor plane"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        structD.src = src
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
        If task.frameCount > 30 Then yCoordinate = kalman.kAverage

        floorYplane = structD.side2D.meterMax * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)

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
        If task.intermediateReview = caller Then task.intermediateObject = Me

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
    Public Sub New()
        initParent()
        side2D = New Histogram_SideData
        structD = New Structured_SliceH

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
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
            Dim planeY = side2D.meterMin * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
            If yCoordinate > task.sideCameraPoint.Y Then planeY = side2D.meterMax * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)
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






Public Class Structured_MultiSliceV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public structD As Structured_SliceV
    Public Sub New()
        initParent()

        top2D = New Histogram_TopData
        structD = New Structured_SliceV

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
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
            Dim planeX = top2D.meterMin * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
            If xCoordinate > task.topCameraPoint.X Then planeX = top2D.meterMax * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)
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






Public Class Structured_MultiSlice
    Inherits VBparent
    Public top2D As Histogram_TopData
    Public side2D As Histogram_SideData
    Dim struct As Structured_SliceV
    Public sliceMask As cv.Mat
    Public split() As cv.Mat
    Public Sub New()
        initParent()

        side2D = New Histogram_SideData()
        top2D = New Histogram_TopData()
        struct = New Structured_SliceV()

        task.desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
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
            Dim planeX = top2D.meterMin * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
            If xCoordinate > task.topCameraPoint.X Then planeX = top2D.meterMax * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)
            Dim depthMask As New cv.Mat
            minVal = planeX - thicknessMeters
            maxVal = planeX + thicknessMeters
            cv.Cv2.InRange(split(0).Clone, minVal, maxVal, depthMask)
            sliceMask = depthMask
            sliceMask.SetTo(0, task.noDepthMask)
            dst2.SetTo(255, sliceMask)
        Next

        For yCoordinate = 0 To src.Height - 1 Step stepsize
            Dim planeY = side2D.meterMin * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
            If yCoordinate > task.sideCameraPoint.Y Then planeY = side2D.meterMax * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)
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







Public Class Structured_MultiSliceLines
    Inherits VBparent
    Dim multi As Structured_MultiSlice
    Public ldetect As Line_Basics
    Public Sub New()
        initParent()
        ldetect = New Line_Basics()
        Dim lenSlider = findSlider("Line length threshold in pixels")
        lenSlider.Value = lenSlider.Maximum ' don't need the yellow line...
        multi = New Structured_MultiSlice()
        task.desc = "Detect lines in the multiSlice output"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
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
        If task.intermediateReview = caller Then task.intermediateObject = Me
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
                cv.Cv2.DrawContours(dst2, contours, i, New cv.Scalar(0, 255, 255), 2, task.lineType)
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
        If task.intermediateReview = caller Then task.intermediateObject = Me
        structD.Run()
        dst2 = structD.dst2
        multi.Run()

        Dim col = CInt(structD.offsetSlider.Value)

        Dim cushion = cushionSlider.Value
        Dim rect = New cv.Rect(col, 0, If(col + cushion >= dst2.Width, dst2.Width - col, cushion), dst2.Height - 1)
        Dim minLoc As cv.Point, maxLoc As cv.Point
        multi.top2D.histOutput(rect).MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        dst2.Circle(New cv.Point(col, dst2.Height - maxLoc.Y), 10, cv.Scalar.Red, -1, task.lineType)
        Dim filterZ = maxLoc.Y / dst2.Height * task.maxZ

        Dim maskZplane As New cv.Mat(multi.split(0).Size, cv.MatType.CV_8U)
        If filterZ > 0 Then
            Dim depthMask As New cv.Mat
            minVal = filterZ - 0.05 ' a 10 cm buffer surrounding the z value
            maxVal = filterZ + 0.05
            cv.Cv2.InRange(multi.split(2), minVal, maxVal, depthMask)
            maskZplane = depthMask
        End If

        If filterZ > 0 Then cv.Cv2.BitwiseAnd(multi.sliceMask, maskZplane, maskZplane)

        dst1 = task.color.Clone
        dst1.SetTo(cv.Scalar.White, maskZplane)

        label2 = "Peak histogram count (" + Format(maxVal, "#0") + ") at " + Format(filterZ, "#0.00") + " meters +-" + Format(10 / task.pixelsPerMeter, "#0.00") + " m"
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
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim minLoc As cv.Point, maxLoc As cv.Point
        Static imuPC As cv.Mat
        floor.src = src
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


Public Class Structured_SliceOptions
    Inherits VBparent
    Public Sub New()
        initParent()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Structured Depth slice thickness in pixels", 1, 10, 1)
            sliders.setupTrackBar(1, "Slice step size in pixels (multi-slice option only)", 1, 100, 20)
            sliders.setupTrackBar(2, "Standalone only horizontal slice offset", 0, src.Width - 1, src.Width / 2)
            sliders.setupTrackBar(3, "Standalone only vertical slice offset", 0, src.Width - 1, src.Width / 2)
        End If

        task.desc = "Structured Slice options"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        task.trueText("This algorithm is used to share the horizontal and vertical slice options.")
    End Sub
End Class





Public Class Structured_SliceH
    Inherits VBparent
    Public side2D As Histogram_SideData
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Dim sliceOptions As Structured_SliceOptions
    Public yPlaneOffset As Integer
    Public Sub New()
        initParent()
        side2D = New Histogram_SideData()

        sliceOptions = New Structured_SliceOptions

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Standalone only horizontal slice offset")

        label2 = "Yellow bar is ceiling.  Yellow line is camera level."
        task.desc = "Find and isolate planes (floor and ceiling) in a side view histogram."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        side2D.Run()

        Dim depthShadow = task.noDepthMask
        Dim Split = side2D.gCloud.dst1.Split()

        Dim yCoordinate = CInt(offsetSlider.Value)

        Dim planeY = side2D.meterMin * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
        If yCoordinate > task.sideCameraPoint.Y Then planeY = side2D.meterMax * (yCoordinate - task.sideCameraPoint.Y) / (dst2.Height - task.sideCameraPoint.Y)

        Dim metersPerPixel = Math.Abs(side2D.meterMax - side2D.meterMin) / dst2.Height
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
        label2 = side2D.label2

        dst2 = side2D.dst1.ConvertScaleAbs(255).Threshold(1, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        yPlaneOffset = If(offsetSlider.Value < dst2.Height - cushion, CInt(offsetSlider.Value), dst2.Height - cushion - 1)
        dst2.Circle(New cv.Point(0, task.sideCameraPoint.Y), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        dst2.Line(New cv.Point(0, yPlaneOffset), New cv.Point(dst2.Width, yPlaneOffset), cv.Scalar.Yellow, cushion)
    End Sub
End Class







Public Class Structured_SliceV
    Inherits VBparent
    Public top2D As Histogram_TopData
    Dim sideStruct As Structured_SliceH
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Dim sliceOptions As Structured_SliceOptions
    Public Sub New()
        initParent()
        top2D = New Histogram_TopData()

        sliceOptions = New Structured_SliceOptions

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        offsetSlider = findSlider("Standalone only vertical slice offset")
        offsetSlider.Maximum = src.Width - 1
        offsetSlider.Value = src.Width / 2

        task.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim xCoordinate = offsetSlider.Value
        top2D.Run()

        Dim split = top2D.gCloud.dst1.Split()

        Dim planeX = top2D.meterMin * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
        If xCoordinate > task.topCameraPoint.X Then planeX = top2D.meterMax * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
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

        dst2 = top2D.dst1.Normalize(0, 255, cv.NormTypes.MinMax)
        dst2.ConvertTo(dst2, cv.MatType.CV_8UC1)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2.Circle(New cv.Point(task.topCameraPoint.X, dst2.Height), task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
        Dim offset = CInt(offsetSlider.Value)
        dst2.Line(New cv.Point(offset, 0), New cv.Point(offset, dst2.Height), cv.Scalar.Yellow, cushion)
    End Sub
End Class








Public Class Structured_SliceVStable
    Inherits VBparent
    Public top2D As Histogram_TopData
    Dim structD As Structured_SliceV
    Public cushionSlider As Windows.Forms.TrackBar
    Public offsetSlider As Windows.Forms.TrackBar
    Public sliceMask As cv.Mat
    Public Sub New()
        initParent()
        top2D = New Histogram_TopData
        structD = New Structured_SliceV

        cushionSlider = findSlider("Structured Depth slice thickness in pixels")
        structD.offsetSlider.Value = src.Width / 2

        task.desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim xCoordinate = offsetSlider.Value
        top2D.Run()
        dst2 = top2D.dst1
        Dim split = top2D.gCloud.dst1.Split()

        Dim planeX = top2D.meterMin * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
        If xCoordinate > task.topCameraPoint.X Then planeX = top2D.meterMax * (xCoordinate - task.topCameraPoint.X) / (dst2.Width - task.topCameraPoint.X)

        Dim metersPerPixel = Math.Abs(top2D.meterMax - top2D.meterMin) / dst2.Height
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








Public Class Structured_CenterSlice
    Inherits VBparent
    Dim vSlice As Structured_SliceV
    Dim line As Line_Basics
    Public topPt As cv.Point2f, botPt As cv.Point2f
    Public slope As Single
    Public avgPt As cv.Point2f
    Public b As Integer
    Public Sub New()
        initParent()
        vSlice = New Structured_SliceV
        line = New Line_Basics
        label1 = "Center Slice in yellow"
        label2 = "White = SliceV output, Red Dot is avgPt"
        task.desc = "Find the vertical center line with accurate depth data.."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

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
        dst2.Circle(avgPt, task.dotSize, cv.Scalar.Red, -1, task.lineType)
        dst2.Line(topPt, botPt, cv.Scalar.Red, 1, task.lineType)
        dst1.Line(topPt, botPt, cv.Scalar.Yellow, 1, task.lineType)
    End Sub
End Class








Public Class Structured_CloudFail
    Inherits VBparent
    Dim mmPixel As Pixel_Measure
    Public Sub New()
        initParent()
        mmPixel = New Pixel_Measure
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Lines in X-Direction", 0, 200, 50)
            sliders.setupTrackBar(1, "Lines in Y-Direction", 0, 200, 50)
            sliders.setupTrackBar(2, "Continuity threshold in mm", 0, 100, 10)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
            check.Box(0).Text = "Impose constraints on X"
            check.Box(1).Text = "Impose constraints on Y"
            check.Box(2).Text = "Impose constraints on neither"
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If

        task.desc = "Attempt to impose a structure on the point cloud data."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static xLineSlider = findSlider("Lines in X-Direction")
        Static yLineSlider = findSlider("Lines in Y-Direction")
        Static thresholdSlider = findSlider("Continuity threshold in mm")
        Dim xLines = xLineSlider.value
        Dim yLines = yLineSlider.value
        Dim threshold = thresholdSlider.value

        Static xCheck = findCheckBox("Impose constraints on X")
        Static yCheck = findCheckBox("Impose constraints on Y")
        Static noCheck = findCheckBox("Impose constraints on neither")
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








Public Class Structured_Cloud
    Inherits VBparent
    Public data As New cv.Mat
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of slices", 0, 200, 100)
            sliders.setupTrackBar(1, "Slice index X", 1, 200, 50)
            sliders.setupTrackBar(2, "Slice index Y", 1, 200, 50)
        End If

        task.desc = "Attempt to impose a linear structure on the pointcloud."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
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








Public Class Structured_Crosshairs
    Inherits VBparent
    Dim sCloud As Structured_Cloud
    Public Sub New()
        initParent()
        sCloud = New Structured_Cloud
        task.desc = "Connect vertical and horizontal dots that are in the same column and row."
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static sliceSlider = findSlider("Number of slices")
        Static xSlider = findSlider("Slice index X")
        Static ySlider = findSlider("Slice index Y")
        Dim xLines = sliceSlider.value
        Dim yLines = CInt(xLines * dst1.Width / dst1.Height)
        Dim indexX = xSlider.value
        Dim indexY = ySlider.value
        If indexX > xLines Then indexX = xLines - 1
        If indexY > yLines Then indexY = yLines - 1

        sCloud.Run()
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
                            dst1.Line(p1, p2, cv.Scalar.White, 2, task.lineType)
                        End If
                    End If
                    If y = indexY Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y, x - 1), pointY.Get(Of Integer)(y, x - 1))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst1.Line(p1, p2, cv.Scalar.White, 2, task.lineType)
                        End If
                    End If
                End If
            Next
        Next
    End Sub
End Class