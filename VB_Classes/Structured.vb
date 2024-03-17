Imports OpenCvSharp.Flann
Imports cv = OpenCvSharp
Public Class Structured_LinearizeFloor : Inherits VB_Algorithm
    Public floor As New Structured_FloorCeiling
    Dim kalman As New Kalman_VB_Basics
    Public sliceMask As cv.Mat
    Public floorYPlane As Single
    Public Sub New()
        If check.Setup(traceName) Then
            check.addCheckBox("Smooth in X-direction")
            check.addCheckBox("Smooth in Y-direction")
            check.addCheckBox("Smooth in Z-direction")
            check.Box(1).Checked = True
        End If
        desc = "Using the mask for the floor create a better representation of the floor plane"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static xCheck = findCheckBox("Smooth in X-direction")
        Static yCheck = findCheckBox("Smooth in Y-direction")
        Static zCheck = findCheckBox("Smooth in Z-direction")
        floor.Run(src)
        dst2 = floor.dst2
        dst3 = floor.dst3
        sliceMask = floor.slice.sliceMask

        Dim imuPC = task.pointCloud.Clone
        imuPC.SetTo(0, Not sliceMask)

        If sliceMask.CountNonZero > 0 Then
            Dim split = imuPC.Split()
            If xCheck.Checked Then
                Dim mm as mmData = vbMinMax(split(0), sliceMask)

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

            If yCheck.Checked Then
                Dim mm as mmData = vbMinMax(split(1), sliceMask)
                kalman.kInput = (mm.minVal + mm.maxVal) / 2
                kalman.Run(src)
                floorYPlane = kalman.kAverage
                split(1).SetTo(floorYPlane, sliceMask)
            End If

            If zCheck.Checked Then
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







Public Class Structured_MultiSlice : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Public sliceMask As cv.Mat
    Public split() As cv.Mat
    Public options As New Options_Structured
    Public Sub New()
        desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim stepSize = options.stepSize

        heat.Run(src)

        split = task.pointCloud.Split()

        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For xCoordinate = 0 To src.Width - 1 Step stepSize
            Dim planeX = -task.xRange * (task.topCameraPoint.X - xCoordinate) / task.topCameraPoint.X
            If xCoordinate > task.topCameraPoint.X Then planeX = task.xRange * (xCoordinate - task.topCameraPoint.X) / (dst3.Width - task.topCameraPoint.X)
            Dim depthMask As New cv.Mat
            Dim minVal As Double, maxVal As Double
            minVal = planeX - task.metersPerPixel
            maxVal = planeX + task.metersPerPixel
            cv.Cv2.InRange(split(0).Clone, minVal, maxVal, depthMask)
            sliceMask = depthMask
            If minVal < 0 And maxVal > 0 Then sliceMask.SetTo(0, task.noDepthMask)
            dst3.SetTo(255, sliceMask)
        Next

        For yCoordinate = 0 To src.Height - 1 Step stepSize
            Dim planeY = -task.yRange * (task.sideCameraPoint.Y - yCoordinate) / task.sideCameraPoint.Y
            If yCoordinate > task.sideCameraPoint.Y Then planeY = task.yRange * (yCoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)
            Dim depthMask As New cv.Mat
            Dim minVal As Double, maxVal As Double
            minVal = planeY - task.metersPerPixel
            maxVal = planeY + task.metersPerPixel
            cv.Cv2.InRange(split(1).Clone, minVal, maxVal, depthMask)
            Dim tmp = depthMask
            sliceMask = tmp Or sliceMask
            dst3.SetTo(255, sliceMask)
        Next

        dst2 = task.color.Clone
        dst2.SetTo(cv.Scalar.White, dst3)
    End Sub
End Class










Public Class Structured_MultiSliceLines : Inherits VB_Algorithm
    Dim multi As New Structured_MultiSlice
    Public lines As New Line_Basics
    Public Sub New()
        desc = "Detect lines in the multiSlice output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        multi.Run(src)
        dst3 = multi.dst3
        lines.Run(dst3)
        dst2 = lines.dst2
    End Sub
End Class







Public Class Structured_MultiSlicePolygon : Inherits VB_Algorithm
    Dim multi As New Structured_MultiSlice
    Public Sub New()
        labels(2) = "Input to FindContours"
        labels(3) = "ApproxPolyDP 4-corner object from FindContours input"

        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Max number of sides in the identified polygons", 3, 100, 4)
        End If
        desc = "Detect polygons in the multiSlice output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static sidesSlider = findSlider("Max number of sides in the identified polygons")
        Dim maxSides = sidesSlider.Value

        multi.Run(src)
        dst2 = Not multi.dst3

        Dim rawContours = cv.Cv2.FindContoursAsArray(dst2, cv.RetrievalModes.Tree, cv.ContourApproximationModes.ApproxSimple)
        Dim contours(rawContours.Length - 1)() As cv.Point
        For j = 0 To rawContours.Length - 1
            contours(j) = cv.Cv2.ApproxPolyDP(rawContours(j), 3, True)
        Next

        dst3.SetTo(0)
        For i = 0 To contours.Length - 1
            If contours(i).Length = 2 Then Continue For
            If contours(i).Length <= maxSides Then
                cv.Cv2.DrawContours(dst3, contours, i, New cv.Scalar(0, 255, 255), task.lineWidth + 1, task.lineType)
            End If
        Next
    End Sub
End Class





Public Class Structured_Depth : Inherits VB_Algorithm
    Dim sliceH As New Structured_SliceH
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst0.Checked = True
        labels = {"", "", "Use mouse to explore slices", "Top down view of the highlighted slice (at left)"}
        desc = "Use the structured depth to enhance the depth away from the centerline."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        sliceH.Run(src)
        dst0 = sliceH.dst3
        dst2 = sliceH.dst2

        Dim mask = sliceH.sliceMask
        Dim perMeter = dst3.Height / task.maxZmeters
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







Public Class Structured_Rebuild : Inherits VB_Algorithm
    Dim heat As New HeatMap_Basics
    Dim options As New Options_Structured
    Dim thickness As Single
    Public pointcloud As New cv.Mat
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Show original data")
            radio.addRadio("Show rebuilt data")
            radio.check(1).Checked = True
        End If

        labels = {"", "", "X values in point cloud", "Y values in point cloud"}
        desc = "Rebuild the point cloud using inrange - not useful yet"
    End Sub
    Private Function rebuildX(viewX As cv.Mat) As cv.Mat
        Dim output As New cv.Mat(task.pcSplit(1).Size, cv.MatType.CV_32F, 0)
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
        Dim output As New cv.Mat(task.pcSplit(1).Size, cv.MatType.CV_32F, 0)
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
    Public Sub RunVB(src As cv.Mat)
        Static rebuiltRadio = findRadio("Show rebuilt data")
        Static originalRadio = findRadio("Show original data")

        options.RunVB()
        Dim metersPerPixel = task.maxZmeters / dst3.Height
        thickness = options.sliceSize * metersPerPixel
        heat.Run(src)

        If rebuiltRadio.checked Then
            task.pcSplit(0) = rebuildX(heat.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            task.pcSplit(1) = rebuildY(heat.dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
            cv.Cv2.Merge(task.pcSplit, pointcloud)
        ElseIf originalRadio.checked Then
            task.pcSplit = task.pointCloud.Split()
            pointcloud = task.pointCloud
        End If

        dst2 = vbNormalize32f(task.pcSplit(0))
        dst3 = vbNormalize32f(task.pcSplit(1))
        dst2.SetTo(0, task.noDepthMask)
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class Structured_Cloud2 : Inherits VB_Algorithm
    Dim mmPixel As New Pixel_Measure
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Lines in X-Direction", 0, 200, 50)
            sliders.setupTrackBar("Lines in Y-Direction", 0, 200, 50)
            sliders.setupTrackBar("Continuity threshold in mm", 0, 100, 10)
        End If

        If check.Setup(traceName) Then
            check.addCheckBox("Impose constraints on X")
            check.addCheckBox("Impose constraints on Y")
            check.Box(0).Checked = True
            check.Box(1).Checked = True
        End If

        desc = "Attempt to impose a structure on the point cloud data."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static xLineSlider = findSlider("Lines in X-Direction")
        Static yLineSlider = findSlider("Lines in Y-Direction")
        Static thresholdSlider = findSlider("Continuity threshold in mm")

        Static xCheck = findCheckBox("Impose constraints on X")
        Static yCheck = findCheckBox("Impose constraints on Y")

        Dim xLines = xLineSlider.Value
        Dim yLines = yLineSlider.Value
        Dim threshold = thresholdSlider.Value

        Dim xconstraint = xCheck.checked
        Dim yconstraint = yCheck.checked

        Dim input = src
        If input.Type <> cv.MatType.CV_32F Then input = task.pcSplit(2)

        Dim stepX = dst2.Width / xLines
        Dim stepY = dst2.Height / yLines
        dst3 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC3, 0)
        Dim midX = dst2.Width / 2
        Dim midY = dst2.Height / 2
        Dim halfStepX = stepX / 2
        Dim halfStepy = stepY / 2
        For y = 1 To yLines - 2
            For x = 1 To xLines - 2
                Dim p1 = New cv.Point2f(x * stepX, y * stepY)
                Dim p2 = New cv.Point2f((x + 1) * stepX, y * stepY)
                Dim d1 = task.pcSplit(2).Get(Of Single)(p1.Y, p1.X)
                Dim d2 = task.pcSplit(2).Get(Of Single)(p2.Y, p2.X)
                If stepX * threshold > Math.Abs(d1 - d2) And d1 > 0 And d2 > 0 Then
                    Dim p = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                    Dim mmPP = mmPixel.Compute(d1)
                    If xconstraint Then
                        p(0) = (p1.X - midX) * mmPP
                        If p1.X = midX Then p(0) = mmPP
                    End If
                    If yconstraint Then
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
        dst2 = dst3(New cv.Rect(0, 0, xLines, yLines)).Resize(dst2.Size, 0, 0, cv.InterpolationFlags.Nearest)
    End Sub
End Class









Public Class Structured_Crosshairs : Inherits VB_Algorithm
    Dim sCloud As New Structured_Cloud
    Public Sub New()
        desc = "Connect vertical and horizontal dots that are in the same column and row."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static sliceSlider = findSlider("Number of slices")
        Static xSlider = findSlider("Slice index X")
        Static ySlider = findSlider("Slice index Y")
        Dim xLines = sliceSlider.Value
        Dim yLines = CInt(xLines * dst2.Width / dst2.Height)
        Dim indexX = xSlider.Value
        Dim indexY = ySlider.Value
        If indexX > xLines Then indexX = xLines - 1
        If indexY > yLines Then indexY = yLines - 1

        sCloud.Run(src)
        Dim split = cv.Cv2.Split(sCloud.dst2)

        Static minX As Single, maxX As Single, minY As Single, maxY As Single
        Dim mmX = vbMinMax(split(0))
        Dim mmY = vbMinMax(split(1))

        minX = If(minX > mmX.minVal, mmX.minVal, minX)
        minY = If(minY > mmY.minVal, mmY.minVal, minY)
        maxX = If(maxX < mmX.maxVal, mmX.maxVal, maxX)
        maxY = If(maxY < mmY.maxVal, mmY.maxVal, maxY)

        setTrueText("mmx min/max = " + Format(minX, "0.00") + "/" + Format(maxX, "0.00") + " mmy min/max " + Format(minY, "0.00") +
                    "/" + Format(maxY, "0.00"), 3)

        dst2.SetTo(0)
        Dim white = New cv.Vec3b(255, 255, 255)
        Dim pointX As New cv.Mat(sCloud.dst2.Size, cv.MatType.CV_32S, 0)
        Dim pointY As New cv.Mat(sCloud.dst2.Size, cv.MatType.CV_32S, 0)
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
                    If x = indexX Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y - 1, x), pointY.Get(Of Integer)(y - 1, x))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst2.Line(p1, p2, task.highlightColor, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                    If y = indexY Then
                        Dim p1 = New cv.Point(pointX.Get(Of Integer)(y, x - 1), pointY.Get(Of Integer)(y, x - 1))
                        If p1.X > 0 Then
                            Dim p2 = New cv.Point(xx, yy)
                            dst2.Line(p1, p2, task.highlightColor, task.lineWidth + 1, task.lineType)
                        End If
                    End If
                End If
            Next
        Next
    End Sub
End Class







Public Class Structured_Cloud : Inherits VB_Algorithm
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("Number of slices", 0, 200, 35)
            sliders.setupTrackBar("Slice index X", 1, 200, 50)
            sliders.setupTrackBar("Slice index Y", 1, 200, 50)
        End If

        gOptions.GridSize.Value = 10
        desc = "Attempt to impose a linear structure on the pointcloud."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static sliceSlider = findSlider("Number of slices")
        Dim xLines = sliceSlider.Value
        Dim yLines = CInt(xLines * dst2.Height / dst2.Width)

        Dim stepX = dst3.Width / xLines
        Dim stepY = dst3.Height / yLines
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        For y = 0 To yLines - 1
            For x = 0 To xLines - 1
                Dim r = New cv.Rect(x * stepX, y * stepY, stepX - 1, stepY - 1)
                Dim p1 = New cv.Point(r.X, r.Y)
                Dim p2 = New cv.Point(r.X + r.Width, r.Y + r.Height)
                Dim vec1 = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                Dim vec2 = task.pointCloud.Get(Of cv.Vec3f)(p2.Y, p2.X)
                If vec1(2) > 0 And vec2(2) > 0 Then dst2(r).SetTo(vec1)
            Next
        Next
        labels(2) = "Structured_Cloud with " + CStr(yLines) + " rows " + CStr(xLines) + " columns"
    End Sub
End Class







Public Class Structured_ROI : Inherits VB_Algorithm
    Public data As New cv.Mat
    Public oglData As New List(Of cv.Point3f)
    Public Sub New()
        gOptions.GridSize.Value = 10
        desc = "Simplify the point cloud so it can be represented as quads in OpenGL"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        For Each roi In task.gridList
            Dim d = task.pointCloud(roi).Mean(task.depthMask(roi))
            Dim depth = New cv.Vec3f(d.Val0, d.Val1, d.Val2)
            Dim pt = New cv.Point(roi.X + roi.Width / 2, roi.Y + roi.Height / 2)
            Dim vec = task.pointCloud.Get(Of cv.Vec3f)(pt.Y, pt.X)
            If vec(2) > 0 Then dst2(roi).SetTo(depth)
        Next

        labels(2) = traceName + " with " + CStr(task.gridList.Count) + " regions was created"
    End Sub
End Class







Public Class Structured_Tiles : Inherits VB_Algorithm
    Public oglData As New List(Of cv.Vec3f)
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        gOptions.GridSize.Value = 10
        desc = "Use the OpenGL point size to represent the point cloud as data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst3

        dst3.SetTo(0)
        oglData.Clear()
        For Each roi In task.gridList
            Dim c = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
            If c = black Then Continue For
            oglData.Add(New cv.Vec3f(c(2) / 255, c(1) / 255, c(0) / 255))

            Dim v = task.pointCloud(roi).Mean(task.depthMask(roi))
            oglData.Add(New cv.Vec3f(v.Val0, v.Val1, v.Val2))
            dst3(roi).SetTo(c)
        Next
        labels(2) = traceName + " with " + CStr(task.gridList.Count) + " regions was created"
    End Sub
End Class








Public Class Structured_TilesQuad : Inherits VB_Algorithm
    Public oglData As New List(Of cv.Vec3f)
    Dim options As New Options_OpenGLFunctions
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        gOptions.GridSize.Value = 10
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32FC3, 0)
        labels = {"", "RedCloud cells", "Simplified depth map - CV_32FC3", "Simplified depth map with RedCloud cell colors"}
        desc = "Simplify the OpenGL quads without using OpenGL's point size"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Options.RunVB()
        hulls.Run(src)
        dst2 = hulls.dst2

        oglData.Clear()
        dst1.SetTo(0)
        dst3.SetTo(0)
        Dim vec As cv.Scalar
        For Each roi In task.gridList
            Dim c = dst2.Get(Of cv.Vec3b)(roi.Y, roi.X)
            If standaloneTest() Then dst3(roi).SetTo(c)
            If c = black Then Continue For

            oglData.Add(New cv.Vec3f(c(2) / 255, c(1) / 255, c(0) / 255))

            Dim v = task.pointCloud(roi).Mean(task.depthMask(roi))
            vec = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y, v(2))) + options.moveAmount
            oglData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y, v(2))) + options.moveAmount
            oglData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X + roi.Width, roi.Y + roi.Height, v(2))) + options.moveAmount
            oglData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))

            vec = getWorldCoordinates(New cv.Point3f(roi.X, roi.Y + roi.Height, v(2))) + options.moveAmount
            oglData.Add(New cv.Point3f(vec.Val0, vec.Val1, vec.Val2))
            If standaloneTest() Then dst1(roi).SetTo(v)
        Next
        labels(2) = traceName + " with " + CStr(task.gridList.Count) + " regions was created"
    End Sub
End Class






Public Class Structured_CountTop : Inherits VB_Algorithm
    Dim slice As New Structured_SliceV
    Dim plot As New Plot_Histogram
    Dim counts As New List(Of Single)
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
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
    Public Sub RunVB(src As cv.Mat)
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
        dst2.SetTo(cv.Scalar.White, dst0)
        dst1.Line(New cv.Point(index, 0), New cv.Point(index, dst1.Height), cv.Scalar.Red,
                  slice.options.sliceSize)

        Dim hist As New cv.Mat(dst0.Width, 1, cv.MatType.CV_32F, counts.ToArray)
        plot.Run(hist)

        dst3 = plot.dst2
    End Sub
End Class






Public Class Structured_FeatureLines : Inherits VB_Algorithm
    Dim struct As New Structured_MultiSlice
    Dim lines As New Feature_Lines
    Public Sub New()
        desc = "Find the lines in the Structured_MultiSlice algorithm output"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        struct.Run(src)
        dst2 = struct.dst2

        lines.Run(struct.dst2)
        dst3 = src.Clone
        For i = 0 To lines.lines2D.Count - 1 Step 2
            Dim p1 = lines.lines2D(i), p2 = lines.lines2D(i + 1)
            dst3.Line(p1, p2, cv.Scalar.Yellow, task.lineWidth, task.lineType)
        Next
    End Sub
End Class






Public Class Structured_FloorCeiling : Inherits VB_Algorithm
    Public slice As New Structured_SliceEither
    Dim kalman As New Kalman_Basics
    Public Sub New()
        ReDim kalman.kInput(2 - 1)
        findCheckBox("Top View (Unchecked Side View)").Checked = False
        desc = "Find the floor or ceiling plane"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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

        kalman.kInput(0) = floorY
        kalman.kInput(1) = ceilingY
        kalman.Run(src)

        labels(2) = "Current slice is at row =" + CStr(task.mouseMovePoint.Y)
        labels(3) = "Ceiling is at row =" + CStr(CInt(kalman.kOutput(1))) + " floor at y=" + CStr(CInt(kalman.kOutput(0)))

        dst2.Line(New cv.Point(0, floorY), New cv.Point(dst2.Width, floorY), cv.Scalar.Yellow, task.lineWidth)
        setTrueText("floor", New cv.Point(10, floorY + task.dotSize), 3)

        Dim rect = New cv.Rect(0, Math.Max(ceilingY - 5, 0), dst2.Width, 10)
        Dim mask = slice.heat.dst3(rect)
        Dim mean As cv.Scalar, stdev As cv.Scalar
        cv.Cv2.MeanStdDev(mask, mean, stdev)
        If mean(0) < mean(2) Then
            dst2.Line(New cv.Point(0, ceilingY), New cv.Point(dst2.Width, ceilingY), cv.Scalar.Yellow, task.lineWidth)
            setTrueText("ceiling", New cv.Point(10, ceilingY + task.dotSize), 3)
        Else
            setTrueText("Ceiling does not appear to be present", 3)
        End If
    End Sub
End Class







Public Class Structured_MultiSliceH : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Public sliceMask As cv.Mat
    Dim options As New Options_Structured
    Public Sub New()
        findCheckBox("Top View (Unchecked Side View)").Checked = False
        desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim stepsize = options.stepSize

        heat.Run(src)
        dst3 = heat.dst3

        sliceMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
        dst2.SetTo(cv.Scalar.White, sliceMask)
        labels(3) = heat.labels(3)
    End Sub
End Class






Public Class Structured_MultiSliceV : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Dim options As New Options_Structured
    Public Sub New()
        findCheckBox("Top View (Unchecked Side View)").Checked = True
        desc = "Use slices through the point cloud to find straight lines indicating planes present in the depth data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim stepsize = options.stepSize

        heat.Run(src)
        dst3 = heat.dst2

        Dim sliceMask = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
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
        dst2.SetTo(cv.Scalar.White, sliceMask)
        labels(3) = heat.labels(3)
    End Sub
End Class







Public Class Structured_SliceXPlot : Inherits VB_Algorithm
    Dim multi As New Structured_MultiSlice
    Dim options As New Options_Structured
    Public Sub New()
        desc = "Find any plane around a peak value in the top-down histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        multi.Run(src)
        dst3 = multi.heat.dst2

        Dim col = If(task.mouseMovePoint.X = 0, dst2.Width / 2, task.mouseMovePoint.X)

        Dim rect = New cv.Rect(col, 0, If(col + options.sliceSize >= dst3.Width, dst3.Width - col,
                               options.sliceSize), dst3.Height - 1)
        Dim mm as mmData = vbMinMax(multi.heat.topframes.dst2(rect))

        dst3.Circle(New cv.Point(col, mm.maxLoc.Y), task.dotSize + 3, cv.Scalar.Yellow, -1, task.lineType)

        dst2 = task.color.Clone
        Dim filterZ = (dst3.Height - mm.maxLoc.Y) / dst3.Height * task.maxZmeters
        If filterZ > 0 Then
            Dim depthMask = multi.split(2).InRange(filterZ - 0.05, filterZ + 0.05) ' a 10 cm buffer surrounding the z value
            depthMask = multi.sliceMask And depthMask
            dst2.SetTo(cv.Scalar.White, depthMask)
        End If

        labels(3) = "Peak histogram count (" + Format(mm.maxVal, fmt0) + ") at " + Format(filterZ, fmt2) + " meters +-" + Format(5 / dst2.Height / task.maxZmeters, fmt2) + " m"
        setTrueText("Use the mouse to move the yellow dot above.", New cv.Point(10, dst2.Height * 7 / 8), 3)
    End Sub
End Class







Public Class Structured_SliceYPlot : Inherits VB_Algorithm
    Dim multi As New Structured_MultiSlice
    Dim options As New Options_Structured
    Public Sub New()
        desc = "Find any plane around a peak value in the side view histogram"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        multi.Run(src)
        dst3 = multi.heat.dst3

        Dim row = If(task.mouseMovePoint.Y = 0, dst2.Height / 2, task.mouseMovePoint.Y)

        Dim rect = New cv.Rect(0, row, dst3.Width - 1, If(row + options.sliceSize >= dst3.Height,
                               dst3.Height - row, options.sliceSize))
        Dim mm as mmData = vbMinMax(multi.heat.sideframes.dst2(rect))

        If mm.maxVal > 0 Then
            dst3.Circle(New cv.Point(mm.maxLoc.X, row), task.dotSize + 3, cv.Scalar.Yellow, -1, task.lineType)
            ' dst3.Line(New cv.Point(mm.maxLoc.X, 0), New cv.Point(mm.maxLoc.X, dst3.Height), task.highlightColor, task.lineWidth, task.lineType)
            Dim filterZ = mm.maxLoc.X / dst3.Width * task.maxZmeters

            Dim depthMask = multi.split(2).InRange(filterZ - 0.05, filterZ + 0.05) ' a 10 cm buffer surrounding the z value
            dst2 = task.color.Clone
            dst2.SetTo(cv.Scalar.White, depthMask)
            Dim pixelsPerMeter = dst2.Width / task.maxZmeters
            labels(3) = "Peak histogram count (" + Format(mm.maxVal, fmt0) + ") at " + Format(filterZ, fmt2) + " meters +-" + Format(5 / pixelsPerMeter, fmt2) + " m"
        End If
        setTrueText("Use the mouse to move the yellow dot above.", New cv.Point(10, dst2.Height * 7 / 8), 3)
    End Sub
End Class








Public Class Structured_MouseSlice : Inherits VB_Algorithm
    Dim slice As New Structured_SliceEither
    Dim lines As New Line_Basics
    Public Sub New()
        labels(2) = "Center Slice in yellow"
        labels(3) = "White = SliceV output, Red Dot is avgPt"
        desc = "Find the vertical center line with accurate depth data.."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.mouseMovePoint = New cv.Point Then task.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
        slice.Run(src)

        lines.Run(slice.sliceMask)
        Dim tops As New List(Of Integer)
        Dim bots As New List(Of Integer)
        Dim topsList As New List(Of cv.Point)
        Dim botsList As New List(Of cv.Point)
        If lines.lpList.Count > 0 Then
            dst3 = lines.dst2
            For Each lp In lines.lpList
                dst3.Line(lp.p1, lp.p2, task.highlightColor, task.lineWidth + 3, task.lineType)
                tops.Add(If(lp.p1.Y < lp.p2.Y, lp.p1.Y, lp.p2.Y))
                bots.Add(If(lp.p1.Y > lp.p2.Y, lp.p1.Y, lp.p2.Y))
                topsList.Add(lp.p1)
                botsList.Add(lp.p2)
            Next

            'Dim topPt = topsList(tops.IndexOf(tops.Min))
            'Dim botPt = botsList(bots.IndexOf(bots.Max))
            'dst3.Circle(New cv.Point2f((topPt.X + botPt.X) / 2, (topPt.Y + botPt.Y) / 2), task.dotSize + 5, cv.Scalar.Red, -1, task.lineType)
            'dst3.Line(topPt, botPt, cv.Scalar.Red, task.lineWidth, task.lineType)
            'dst2.Line(topPt, botPt, task.highlightColor, task.lineWidth + 2, task.lineType)
        End If
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(cv.Scalar.White, dst3)
        End If
    End Sub
End Class







Public Class Structured_SliceEither : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Public sliceMask As New cv.Mat
    Dim options As New Options_Structured
    Public Sub New()
        findCheckBox("Top View (Unchecked Side View)").Checked = False
        desc = "Create slices in top and side views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Static topRadio = findCheckBox("Top View (Unchecked Side View)")
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
        dst3.Circle(New cv.Point(task.topCameraPoint.X, dst3.Height), task.dotSize,
                    cv.Scalar.Yellow, -1, task.lineType)
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
            dst2.SetTo(cv.Scalar.White, sliceMask)
        End If
    End Sub
End Class








Public Class Structured_SliceV : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Public sliceMask As New cv.Mat
    Public options As New Options_Structured
    Public Sub New()
        findCheckBox("Top View (Unchecked Side View)").Checked = True
        desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If task.mouseMovePoint = New cv.Point Then task.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
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
        dst3.Circle(New cv.Point(task.topCameraPoint.X, dst3.Height), task.dotSize, task.highlightColor, -1, task.lineType)
        dst3.Line(New cv.Point(xCoordinate, 0), New cv.Point(xCoordinate, dst3.Height), task.highlightColor,
                  options.sliceSize)
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(cv.Scalar.White, sliceMask)
        End If
    End Sub
End Class









Public Class Structured_TransformH : Inherits VB_Algorithm
    Dim options As New Options_Structured
    Dim histTop As New Histogram2D_Top
    Public Sub New()
        labels(3) = "Top down view of the slice of the point cloud"
        desc = "Find and isolate planes (floor and ceiling) in a TopView or SideView histogram."
    End Sub
    Public Function createSliceMaskH() As cv.Mat
        options.RunVB()

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
    Public Sub RunVB(src As cv.Mat)
        Dim sliceMask = createSliceMaskH()

        histTop.Run(task.pointCloud.SetTo(0, Not sliceMask))
        dst3 = histTop.dst2

        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(cv.Scalar.White, sliceMask)
        End If
    End Sub
End Class






Public Class Structured_TransformV : Inherits VB_Algorithm
    Dim options As New Options_Structured
    Dim histSide As New Histogram2D_Side
    Public Sub New()
        labels(3) = "Side view of the slice of the point cloud"
        desc = "Find and isolate planes using the top view histogram data"
    End Sub
    Public Function createSliceMaskV() As cv.Mat
        options.RunVB()

        Dim sliceMask As New cv.Mat
        If task.mouseMovePoint = New cv.Point Then task.mouseMovePoint = New cv.Point(dst2.Width / 2, dst2.Height)
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
    Public Sub RunVB(src As cv.Mat)
        Dim sliceMask = createSliceMaskV()

        histSide.Run(task.pointCloud.SetTo(0, Not sliceMask))
        dst3 = histSide.dst2

        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(cv.Scalar.White, sliceMask)
        End If
    End Sub
End Class








Public Class Structured_SliceH : Inherits VB_Algorithm
    Public heat As New HeatMap_Basics
    Public sliceMask As New cv.Mat
    Public options As New Options_Structured
    Public ycoordinate As Integer
    Public Sub New()
        desc = "Find and isolate planes (floor and ceiling) in a TopView or SideView histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        heat.Run(src)

        If standaloneTest() Then ycoordinate = If(task.mouseMovePoint.Y = 0, dst2.Height / 2, task.mouseMovePoint.Y)

        Dim planeY = -task.yRange * (task.sideCameraPoint.Y - ycoordinate) / task.sideCameraPoint.Y
        If ycoordinate > task.sideCameraPoint.Y Then planeY = task.yRange * (ycoordinate - task.sideCameraPoint.Y) / (dst3.Height - task.sideCameraPoint.Y)

        Dim thicknessMeters = options.sliceSize * task.metersPerPixel
        Dim minVal = planeY - thicknessMeters
        Dim maxVal = planeY + thicknessMeters
        cv.Cv2.InRange(task.pcSplit(1), minVal, maxVal, sliceMask)

        labels(2) = "At offset " + CStr(ycoordinate) + " y = " + Format((maxVal + minVal) / 2, fmt2) +
                    " with " + Format(Math.Abs(maxVal - minVal) * 100, fmt2) + " cm width"
        If minVal <= 0 And maxVal >= 0 Then sliceMask.SetTo(0, task.noDepthMask)
        labels(3) = heat.labels(2)

        dst3 = heat.dst3
        Dim yPlaneOffset = If(ycoordinate < dst3.Height - options.sliceSize, CInt(ycoordinate),
                              dst3.Height - options.sliceSize - 1)
        dst3.Circle(New cv.Point(0, task.sideCameraPoint.Y), task.dotSize, task.highlightColor, -1, task.lineType)
        dst3.Line(New cv.Point(0, yPlaneOffset), New cv.Point(dst3.Width, yPlaneOffset), task.highlightColor,
                  options.sliceSize)
        If standaloneTest() Then
            dst2 = src
            dst2.SetTo(cv.Scalar.White, sliceMask)
        End If
    End Sub
End Class





Public Class Structured_CountSide : Inherits VB_Algorithm
    Dim slice As New Structured_SliceH
    Dim plot As New Plot_Histogram
    Dim rotate As New Rotate_Basics
    Public counts As New List(Of Single)
    Public maxCountIndex As Integer
    Public yValues As New List(Of Single)
    Public Sub New()
        rotate.rotateCenter = New cv.Point2f(dst2.Width / 2, dst2.Width / 2)
        rotate.rotateAngle = -90
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        labels = {"", "Max Slice output - likely flat surface", "Structured Slice heatmap input - red line is max", "Histogram of pixel counts in each slice"}
        desc = "Count the number of pixels found in each slice of the point cloud data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
        dst2.Line(New cv.Point(0, maxCountIndex), New cv.Point(dst2.Width, maxCountIndex),
                  cv.Scalar.Red, slice.options.sliceSize)

        Dim hist As New cv.Mat(dst0.Height, 1, cv.MatType.CV_32F, counts.ToArray)
        plot.dst2 = New cv.Mat(dst2.Height, dst2.Height, cv.MatType.CV_8UC3, 0)
        plot.Run(hist)
        dst3 = plot.dst2

        dst3 = dst3.Resize(New cv.Size(dst2.Width, dst2.Width))
        rotate.Run(dst3)
        dst3 = rotate.dst2
        setTrueText("Max flat surface at: " + vbCrLf + Format(yValues(maxCountIndex), fmt3), 2)
    End Sub
End Class





Public Class Structured_CountSideSum : Inherits VB_Algorithm
    Public counts As New List(Of Single)
    Public maxCountIndex As Integer
    Public yValues As New List(Of Single)
    Public Sub New()
        redOptions.ProjectionThreshold.Value += 50 ' to get the point cloud into the histogram.
        labels = {"", "Max Slice output - likely flat surface", "Structured Slice heatmap input - red line is max", "Histogram of pixel counts in each slice"}
        desc = "Count the number of points found in each slice of the point cloud data."
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
                dst2.Line(New cv.Point(0, i), New cv.Point(dst2.Width, i), cv.Scalar.White, task.lineWidth)
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
        setTrueText(strOut, 2)

        dst3.SetTo(cv.Scalar.Red)
        Dim barHeight = dst2.Height / counts.Count
        For i = 0 To counts.Count - 1
            Dim w = dst2.Width * counts(i) / max
            cv.Cv2.Rectangle(dst3, New cv.Rect(0, i * barHeight, w, barHeight), cv.Scalar.Black, -1)
        Next
    End Sub
End Class