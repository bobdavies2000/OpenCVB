Imports cv = OpenCvSharp
' http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
Public Class Plane_Basics : Inherits TaskParent
    Dim frames As New History_Basics
    Public Sub New()
        labels = {"", "Top down mask after after thresholding heatmap", "Vertical regions", "Horizontal regions"}
        desc = "Find the regions that are mostly vertical and mostly horizontal."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim topHist As New cv.Mat, sideHist As New cv.Mat, topBackP As New cv.Mat, sideBackP As New cv.Mat
        cv.Cv2.CalcHist({task.pointCloud}, task.channelsTop, New cv.Mat, topHist, 2,
                        {dst2.Height, dst2.Width}, task.rangesTop)
        topHist.Row(0).SetTo(0)
        cv.Cv2.InRange(topHist, task.projectionThreshold, topHist.Total, dst1)
        dst1.ConvertTo(dst1, cv.MatType.CV_32F)
        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, dst1, topBackP, task.rangesTop)

        frames.Run(topBackP)
        frames.dst2.ConvertTo(dst2, cv.MatType.CV_8U)

        dst3 = Not dst2
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class








' http://pi.math.cornell.edu/~froh/231f08e1a.pdf
Public Class Plane_From3Points : Inherits TaskParent
    Public input(3 - 1) As cv.Point3f
    Public showWork As Boolean = True
    Public cross As cv.Point3f
    Public k As Single
    Public Sub New()
        labels = {"", "", "Plane Equation", ""}
        input = {New cv.Point3f(2, 1, -1), New cv.Point3f(0, -2, 0), New cv.Point3f(1, -1, 2)}
        desc = "Build a plane equation from 3 points in 3-dimensional space"
    End Sub
    Public Function vbFormatEquation(eq As cv.Vec4f) As String
        Dim s1 = If(eq(1) < 0, " - ", " +")
        Dim s2 = If(eq(2) < 0, " - ", " +")
        Return If(eq(0) < 0, "-", " ") + Format(Math.Abs(eq(0)), fmt3) + "*x " + s1 +
                                         Format(Math.Abs(eq(1)), fmt3) + "*y " + s2 +
                                         Format(Math.Abs(eq(2)), fmt3) + "*z = " +
                                         Format(eq(3), fmt3) + vbCrLf
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim v1 = input(1) - input(0)
        Dim v2 = input(1) - input(2)
        cross = crossProduct(v1, v2)

        ' a*x + b*y + c*z + k = 0 or k = -a*x - b*y - c*z
        k = -cross.X * input(0).X - cross.Y * input(0).Y - cross.Z * input(0).Z
        strOut = "Input: " + vbCrLf
        For i = 0 To input.Count - 1
            strOut += "p" + CStr(i) + " = " + Format(input(i).X, fmt3) + ", " + Format(input(i).Y, fmt3) + ", " + Format(input(i).Z, fmt3) + vbCrLf
        Next

        strOut += "First " + vbTab + "difference = " + Format(v1.X, fmt3) + ", " + Format(v1.Y, fmt3) + ", " + Format(v1.Z, fmt3) + vbCrLf
        strOut += "Second " + vbTab + "difference = " + Format(v2.X, fmt3) + ", " + Format(v2.Y, fmt3) + ", " + Format(v2.Z, fmt3) + vbCrLf
        strOut += "Cross Product = " + Format(cross.X, fmt3) + ", " + Format(cross.Y, fmt3) + ", " + Format(cross.Z, fmt3) + vbCrLf
        strOut += "k = " + CStr(k) + vbCrLf
        strOut += vbFormatEquation(New cv.Vec4f(cross.X, cross.Y, cross.Z, k))
        Dim s1 = If(cross.Y < 0, " - ", " + ")
        Dim s2 = If(cross.Z < 0, " - ", " + ")
        strOut += "Plane equation: " + Format(cross.X, fmt3) + "x" + s1 + Format(Math.Abs(cross.Y), fmt3) + "y" + s2 +
                   Format(Math.Abs(cross.Z), fmt3) + "z + " + Format(-k, fmt3) + vbCrLf
        If showWork Then SetTrueText(strOut, 2)
    End Sub
End Class






Public Class Plane_FlatSurfaces : Inherits TaskParent
    Dim addW As New AddWeighted_Basics
    Dim plane As New Plane_CellColor
    Public Sub New()
        labels = {"RedCloud Cell contours", "", "RedCloud cells", ""}
        addW.src2 = dst2.Clone
        desc = "Find all the cells from a RedColor_Basics output that are likely to be flat"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        plane.Run(src)

        dst2 = plane.dst2
        If task.heartBeat Then addW.src2.SetTo(0)

        Dim flatCount = 0
        For Each rc In task.rcList
            If rc.depth < 1.0 Then Continue For ' close objects look like planes.
            Dim RMSerror As Double = 0
            Dim pixelCount = 0
            For y = 0 To rc.rect.Height - 1
                For x = 0 To rc.rect.Width - 1
                    Dim val = rc.mask.Get(Of Byte)(y, x)
                    If val > 0 Then
                        If msRNG.Next(100) < 10 Then
                            Dim pt = task.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x)
                            ' a*x + b*y + c*z + k = 0 ---> z = -(k + a*x + b*y) / c
                            Dim depth = -(rc.eq(0) * pt.X + rc.eq(1) * pt.Y + rc.eq(3)) / rc.eq(2)
                            RMSerror += Math.Abs(pt.Z - depth)
                            pt.Z = depth
                            pixelCount += 1
                        End If
                    End If
                Next
            Next
            If RMSerror / pixelCount <= plane.options.rmsThreshold Then
                addW.src2(rc.rect).SetTo(white, rc.mask)
                flatCount += 1
            End If
        Next

        addW.Run(task.color)
        dst3 = addW.dst2
        labels(3) = "There were " + CStr(flatCount) + " RedCloud Cells with an average RMSerror per pixel less than " + Format(plane.options.rmsThreshold * 100, fmt0) + " cm"
    End Sub
End Class







'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
Public Class Plane_OnlyPlanes : Inherits TaskParent
    Public plane As New Plane_CellColor
    Public contours As List(Of cv.Point)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_32FC3, 0)
        labels = {"", "", "RedCloud Cells", "gCloud reworked with planes instead of depth data"}
        desc = "Replace the gCloud with planes in every RedCloud cell"
    End Sub
    Public Sub buildCloudPlane(rc As rcData)
        For y = 0 To rc.rect.Height - 1
            For x = 0 To rc.rect.Width - 1
                If rc.mask.Get(Of Byte)(y, x) > 0 Then
                    Dim pt = task.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x)
                    ' a*x + b*y + c*z + k = 0 ---> z = -(k + a*x + b*y) / c
                    pt.Z = -(rc.eq(0) * pt.X + rc.eq(1) * pt.Y + rc.eq(3)) / rc.eq(2)
                    If rc.mmZ.minVal <= pt.Z And rc.mmZ.maxVal >= pt.Z Then
                        dst3(rc.rect).Set(Of cv.Point3f)(y, x, pt)
                    End If
                End If
            Next
        Next
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        plane.Run(src)
        dst2 = plane.dst2

        dst3.SetTo(0)
        For Each rc In task.rcList
            If plane.options.reuseRawDepthData = False Then buildCloudPlane(rc)
        Next
        If plane.options.reuseRawDepthData Then dst3 = task.pointCloud

        Dim rcX = task.rcD
    End Sub
End Class








Public Class Plane_EqCorrelation : Inherits TaskParent
    Dim plane As New Plane_Points
    Public correlations As New List(Of Single)
    Public equations As New List(Of cv.Vec4f)
    Public ptList2D As New List(Of List(Of cv.Point))
    Public Sub New()
        desc = "Classify equations based on the correlation of their coefficients"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        plane.Run(src)
        dst2 = plane.dst2

        If plane.equations.Count = 0 Then
            dst0 = src
            SetTrueText("Select a RedCloud cell to analyze.", 3)
            Exit Sub
        End If

        equations = New List(Of cv.Vec4f)(plane.equations)
        ptList2D = New List(Of List(Of cv.Point))(plane.ptList2D)
        correlations.Clear()

        Dim correlationMat As New cv.Mat
        Dim count(plane.equations.Count - 1) As Integer
        For i = 0 To equations.Count - 1
            Dim p1 = equations(i)
            Dim data1 = cv.Mat.FromPixelData(4, 1, cv.MatType.CV_32F, {p1(0), p1(1), p1(2), p1(3)})

            For j = i + 1 To equations.Count - 1
                Dim p2 = equations(j)
                Dim data2 = cv.Mat.FromPixelData(4, 1, cv.MatType.CV_32F, {p2(0), p2(1), p2(2), p2(3)})
                cv.Cv2.MatchTemplate(data1, data2, correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                Dim correlation = correlationMat.Get(Of Single)(0, 0)
                correlations.Add(correlation)

                If correlation >= 0.999 Then count(i) += 1
            Next
        Next

        Dim countList = New List(Of Integer)(count)
        Dim index = countList.IndexOf(countList.Max)
        Dim pt = equations(index)
        Dim s1 = If(pt(1) < 0, " - ", " + ")
        Dim s2 = If(pt(2) < 0, " - ", " + ")

        If count(index) > plane.equations.Count / 4 Then
            With task.kalman
                .kInput = {pt(0), pt(1), pt(2), pt(3)}
                .Run(src)

                strOut = "Normalized Plane equation: " + Format(.kOutput(0), fmt3) + "x" + s1 + Format(Math.Abs(.kOutput(1)), fmt3) + "y" + s2 +
                         Format(Math.Abs(.kOutput(2)), fmt3) + "z = " + Format(- .kOutput(3), fmt3) + " with " + CStr(count(index)) +
                         " closely matching plane equations." + vbCrLf
            End With
        End If
        SetTrueText(strOut, 3)
    End Sub
End Class







'  http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points
' pyransac-3d on Github - https://github.com/leomariga/pyRANSAC-3D
Public Class Plane_CellColor : Inherits TaskParent
    Public options As New Options_Plane
    Public Sub New()
        labels = {"", "", "RedCloud Cells", "Blue - normal is closest to the X-axis, green - to the Y-axis, and Red - to the Z-axis"}
        desc = "Create a plane equation from the points in each RedCloud cell and color the cell with the direction of the normal"
    End Sub
    Public Function buildContourPoints(rc As rcData) As List(Of cv.Point3f)
        Dim fitPoints As New List(Of cv.Point3f)
        For Each pt In rc.contour
            If pt.X >= rc.rect.Width Or pt.Y >= rc.rect.Height Then Continue For
            If rc.mask.Get(Of Byte)(pt.Y, pt.X) = 0 Then Continue For
            fitPoints.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(pt.Y, pt.X)) ' each contour point is guaranteed to be in the mask and have depth.
        Next
        Return fitPoints
    End Function
    Public Function buildMaskPointEq(rc As rcData) As List(Of cv.Point3f)
        Dim fitPoints As New List(Of cv.Point3f)
        For y = 0 To rc.rect.Height - 1
            For x = 0 To rc.rect.Width - 1
                If rc.mask.Get(Of Byte)(y, x) Then fitPoints.Add(task.pointCloud(rc.rect).Get(Of cv.Point3f)(y, x))
            Next
        Next
        Return fitPoints
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = runRedC(src, labels(2))

        dst3.SetTo(0)
        Dim newCells As New List(Of rcData)
        Dim rcX = task.rcD
        For Each rc In task.rcList
            rc.eq = New cv.Vec4f
            If options.useMaskPoints Then
                rc.eq = fitDepthPlane(buildMaskPointEq(rc))
            ElseIf options.useContourPoints Then
                rc.eq = fitDepthPlane(buildContourPoints(rc))
            ElseIf options.use3Points Then
                rc.eq = build3PointEquation(rc)
            End If
            newCells.Add(rc)
            dst3(rc.rect).SetTo(New cv.Scalar(Math.Abs(255 * rc.eq(0)),
                                              Math.Abs(255 * rc.eq(1)),
                                              Math.Abs(255 * rc.eq(2))), rc.mask)
        Next
        task.rcList = New List(Of rcData)(newCells)
    End Sub
End Class








Public Class Plane_Points : Inherits TaskParent
    Dim plane As New Plane_From3Points
    Public equations As New List(Of cv.Vec4f)
    Public ptList As New List(Of cv.Point3f)
    Public ptList2D As New List(Of List(Of cv.Point))
    Dim needOutput As Boolean
    Public Sub New()
        labels = {"", "", "RedCloud Basics output - click to highlight a cell", ""}
        desc = "Detect if a some or all points in a RedCloud cell are in a plane."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        Dim rc = task.rcD
        labels(2) = "Selected cell has " + CStr(rc.contour.Count) + " points."

        ' this contour will have more depth data behind it.  Simplified contours will lose lots of depth data.
        rc.contour = ContourBuild(rc.mask, cv.ContourApproximationModes.ApproxNone)

        Dim pt As cv.Point3f, list2D As New List(Of cv.Point)
        ptList.Clear()
        For i = 0 To rc.contour.Count - 1
            pt = task.pointCloud.Get(Of cv.Point3f)(rc.contour(i).Y, rc.contour(i).X)
            If pt.Z > 0 Then
                ptList.Add(pt)
                list2D.Add(rc.contour(i))
                If ptList.Count > 100 Then Exit For
            End If
        Next

        If task.heartBeat Or needOutput Then
            ptList2D.Clear()
            equations.Clear()
            needOutput = False
            strOut = ""
            If ptList.Count < 3 Then
                needOutput = True
                strOut = "There weren't enough points in that cell contour with depth.  Select another cell."
            Else
                Dim c = ptList.Count
                For i = 0 To ptList.Count - 1
                    Dim list2Dinput As New List(Of cv.Point)
                    For j = 0 To 3 - 1
                        Dim ptIndex = Choose(j + 1, i, (i + CInt(c / 3)) Mod c, (i + CInt(2 * c / 3)) Mod c)
                        plane.input(j) = ptList(ptIndex)
                        list2Dinput.Add(list2D(ptIndex))
                    Next

                    plane.Run(src)
                    strOut += plane.vbFormatEquation(New cv.Vec4f(plane.cross.X, plane.cross.Y, plane.cross.Z, plane.k))
                    equations.Add(New cv.Vec4f(plane.cross.X, plane.cross.Y, plane.cross.Z, plane.k))
                    ptList2D.Add(list2Dinput)
                Next
            End If
        End If

        SetTrueText(strOut, 3)
    End Sub
End Class








Public Class Plane_Histogram : Inherits TaskParent
    Dim solo As New PointCloud_Solo
    Dim hist As New Hist_Basics
    Public peakCeiling As Single
    Public peakFloor As Single
    Public ceilingPop As Single
    Public floorPop As Single
    Public Sub New()
        labels = {"", "", "Histogram of Y-Values of the point cloud after masking", "Mask used to isolate histogram input"}
        desc = "Create a histogram plot of the Y-values in the backprojection of solo points."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        solo.Run(src)
        dst3 = solo.dst3

        Dim points = dst3.FindNonZero()
        Dim yList As New List(Of Single)
        For i = 0 To points.Rows - 1
            Dim pt = points.Get(Of cv.Point)(i, 0)
            Dim yVal = task.pcSplit(1).Get(Of Single)(pt.Y, pt.X)
            If yVal <> 0 Then yList.Add(yVal)
        Next

        If yList.Count = 0 Then Exit Sub
        hist.mm.minVal = yList.Min
        hist.mm.maxVal = yList.Max
        hist.Run(cv.Mat.FromPixelData(yList.Count, 1, cv.MatType.CV_32F, yList.ToArray))
        dst2 = hist.dst2
        Dim binWidth As Single = dst2.Width / task.histogramBins
        Dim rangePerBin = (hist.mm.maxVal - hist.mm.minVal) / task.histogramBins

        Dim midHist = task.histogramBins / 2
        Dim mm As mmData = GetMinMax(hist.histogram(New cv.Rect(0, midHist, 1, midHist)))
        floorPop = mm.maxVal
        Dim peak = hist.mm.minVal + (midHist + mm.maxLoc.Y + 1) * rangePerBin
        Dim rX As Integer = (midHist + mm.maxLoc.Y) * binWidth
        dst2.Rectangle(New cv.Rect(rX, 0, binWidth, dst2.Height), cv.Scalar.Black, task.lineWidth)
        If Math.Abs(peak - peakCeiling) > rangePerBin Then peakCeiling = peak

        mm = GetMinMax(hist.histogram(New cv.Rect(0, 0, 1, midHist)))
        ceilingPop = mm.maxVal
        peak = hist.mm.minVal + (mm.maxLoc.Y + 1) * rangePerBin
        rX = mm.maxLoc.Y * binWidth
        dst2.Rectangle(New cv.Rect(rX, 0, binWidth, dst2.Height), cv.Scalar.Yellow, task.lineWidth)
        If Math.Abs(peak - peakFloor) > rangePerBin * 2 Then peakFloor = peak

        labels(3) = "Peak Ceiling = " + Format(peakCeiling, fmt3) + " and Peak Floor = " + Format(peakFloor, fmt3)
        SetTrueText("Yellow rectangle is likely floor and black is likely ceiling.")
    End Sub
End Class







' https://stackoverflow.com/questions/33997220/plane-construction-from-3d-points-in-opencv
Public Class Plane_Equation : Inherits TaskParent
    Public rc As New rcData
    Public justEquation As String
    Public Sub New()
        desc = "Compute the coefficients for an estimated plane equation given the rc contour"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If standaloneTest() Then
            dst2 = runRedC(src, labels(2))
            rc = task.rcD
            If rc.index = 0 Then SetTrueText("Select a cell in the image at left.")
        End If

        Dim offset = CInt(rc.contour.Count / 4) - 1
        Dim xList As New List(Of Single)
        Dim yList As New List(Of Single)
        Dim zList As New List(Of Single)
        Dim kList As New List(Of Single)
        Dim dotlist As New List(Of Single)
        For j = 0 To offset - 1
            Dim p1 = rc.contour(j + offset * 0)
            Dim p2 = rc.contour(j + offset * 1)
            Dim p3 = rc.contour(j + offset * 2)
            Dim p4 = rc.contour(j + offset * 3)

            Dim v1 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p1.Y, p1.X)
            Dim v2 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p2.Y, p2.X)
            Dim v3 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p3.Y, p3.X)
            Dim v4 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p4.Y, p4.X)
            Dim cross1 = crossProduct(v1 - v2, v2 - v3)
            Dim cross2 = crossProduct(v1 - v4, v4 - v3)

            Dim dot = dotProduct3D(cross1, cross2)
            dotlist.Add(dot)
            Dim k = -cross1.X * v1.X - cross1.Y * v1.Y - cross1.Z * v1.Z
            xList.Add(cross1.X)
            yList.Add(cross1.Y)
            zList.Add(cross1.Z)
            kList.Add(k)
        Next

        If dotlist.Count Then
            Dim dotIndex = dotlist.IndexOf(dotlist.Max)
            rc.eq = New cv.Vec4f(xList(dotIndex), yList(dotIndex), zList(dotIndex), kList(dotIndex))
        End If
        If dotlist.Count Then
            If task.heartBeat Then
                justEquation = Format(rc.eq(0), fmt3) + "*X + " + Format(rc.eq(1), fmt3) + "*Y + "
                justEquation += Format(rc.eq(2), fmt3) + "*Z + " + Format(rc.eq(3), fmt3) + vbCrLf
                If xList.Count > 0 Then
                    strOut = "The rc.contour has " + CStr(rc.contour.Count) + " points" + vbCrLf
                    strOut += "Estimated 3D plane equation:" + vbCrLf
                    strOut += justEquation + vbCrLf
                Else
                    If strOut.Contains("Insufficient points") = False Then
                        strOut += vbCrLf + "Insufficient points or best dot product too low at " + Format(dotlist.Max(), "0.00")
                    End If
                End If
                strOut += CStr(xList.Count) + " 3D plane equations were tested with an average dot product = " +
                              Format(dotlist.Average, "0.00")
            End If
        End If
        If standaloneTest() Then
            SetTrueText(strOut, 3)
            dst3.SetTo(0)
            DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
        End If
    End Sub
End Class






Public Class Plane_Verticals : Inherits TaskParent
    Dim solo As New PointCloud_Solo
    Dim frames As New History_Basics
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"RGB image with highlights for likely vertical surfaces over X frames.",
                  "Heatmap top view", "Single frame backprojection of red areas in the heatmap",
                  "Thresholded heatmap top view mask"}
        desc = "Use a heatmap to isolate vertical walls - incomplete!"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        solo.Run(src)
        dst3 = solo.heat.topframes.dst2.InRange(task.projectionThreshold * task.frameHistoryCount, dst2.Total)

        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32FC1, 0)
        solo.heat.dst0.CopyTo(dst1, dst3)
        dst1.ConvertTo(dst1, cv.MatType.CV_32FC1)

        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsTop, dst1, dst2, task.rangesTop)

        frames.Run(dst2)
        frames.dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = frames.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst0, cv.MatType.CV_8U)
        task.color.SetTo(white, dst0)
    End Sub
End Class







Public Class Plane_Horizontals : Inherits TaskParent
    Dim solo As New PointCloud_Solo
    Dim frames As New History_Basics
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = true
        labels = {"RGB image with highlights for likely floor or ceiling over X frames.",
                  "Heatmap side view", "Single frame backprojection areas in the heatmap",
                  "Thresholded heatmap side view mask"}
        desc = "Use the solo points to isolate horizontal surfaces - floor or ceiling or table tops."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        solo.Run(src)
        dst3 = solo.heat.sideframes.dst2.InRange(task.projectionThreshold * task.frameHistoryCount, dst2.Total)

        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        solo.heat.dst1.CopyTo(dst1, dst3)
        dst1.ConvertTo(dst1, cv.MatType.CV_32FC1)

        cv.Cv2.CalcBackProject({task.pointCloud}, task.channelsSide, dst1, dst2, task.rangesSide)

        frames.Run(dst2)
        frames.dst2.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = frames.dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst2.ConvertTo(dst0, cv.MatType.CV_8U)
        task.color.SetTo(white, dst0)
    End Sub
End Class









Public Class Plane_FloorStudy : Inherits TaskParent
    Public slice As New Structured_SliceH
    Dim yList As New List(Of Single)
    Public planeY As Single
    Dim options = New Options_PlaneFloor()
    Public Sub New()
        If standalone Then task.gOptions.displaydst1.checked = True
        labels = {"", "", "", ""}
        desc = "Find the floor plane (if present)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        slice.Run(src)
        dst1 = slice.dst3

        dst0 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Dim thicknessCMs = task.metersPerPixel * 1000 / 100, rect As cv.Rect, nextY As Single
        For y = dst0.Height - 2 To 0 Step -1
            rect = New cv.Rect(0, y, dst0.Width - 1, 1)
            Dim count = dst0(rect).CountNonZero
            If count > options.countThreshold Then
                nextY = -task.yRange * (task.sideCameraPoint.Y - y) / task.sideCameraPoint.Y - thicknessCMs / 2.5 ' narrow it down to about 1 cm
                labels(2) = "Y = " + Format(planeY, fmt3) + " separates the floor."
                SetTrueText(labels(2), 3)
                Dim sliceMask = task.pcSplit(1).InRange(cv.Scalar.All(planeY), cv.Scalar.All(3.0))
                dst2 = src
                dst2.SetTo(white, sliceMask)
                Exit For
            End If
        Next

        yList.Add(nextY)
        planeY = yList.Average()
        If yList.Count > 20 Then yList.RemoveAt(0)
        dst1.Line(New cv.Point(0, rect.Y), New cv.Point(dst2.Width, rect.Y), cv.Scalar.Yellow, slice.options.sliceSize, task.lineType)
    End Sub
End Class
