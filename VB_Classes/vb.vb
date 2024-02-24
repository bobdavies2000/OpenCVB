Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Module VB
    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"
    Public Const depthListMaxCount As Integer = 10
    Public Function findCenter(clist As List(Of cv.Point)) As cv.Point2f
        Dim xsum As Integer, ysum As Integer
        For Each pt In clist
            xsum += pt.X
            ysum += pt.Y
        Next
        Return New cv.Point2f(xsum / clist.Count, ysum / clist.Count)
    End Function
    Public Function validContourPoint(rc As rcData, pt As cv.Point, offset As Integer) As cv.Point
        If pt.X < rc.rect.Width And pt.Y < rc.rect.Height Then Return pt
        Dim count = rc.contour.Count
        For i = offset + 1 To rc.contour.Count - 1
            pt = rc.contour(i Mod count)
            If pt.X < rc.rect.Width And pt.Y < rc.rect.Height Then Return pt
        Next
        Return New cv.Point
    End Function
    Public Function build3PointEquation(rc As rcData) As cv.Vec4f
        If rc.contour.Count < 3 Then Return New cv.Vec4f
        Dim offset = rc.contour.Count / 3
        Dim p1 = validContourPoint(rc, rc.contour(offset * 0), offset * 0)
        Dim p2 = validContourPoint(rc, rc.contour(offset * 1), offset * 1)
        Dim p3 = validContourPoint(rc, rc.contour(offset * 2), offset * 2)

        Dim v1 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p1.Y, p1.X)
        Dim v2 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p2.Y, p2.X)
        Dim v3 = task.pointCloud(rc.rect).Get(Of cv.Point3f)(p3.Y, p3.X)

        Dim cross = crossProduct(v1 - v2, v2 - v3)
        Dim k = -(v1.X * cross.X + v1.Y * cross.Y + v1.Z * cross.Z)
        Return New cv.Vec4f(cross.X, cross.Y, cross.Z, k)
    End Function
    Public Function computePlaneEq(contours As List(Of cv.Point), rect As cv.Rect, ByRef bestThreshold As Single) As cv.Vec4f
        Dim offset = CInt(contours.Count / 4) - 1
        Dim eq As cv.Vec4f
        For j = 0 To offset - 1
            Dim p1 = contours(j + offset * 0)
            Dim p2 = contours(j + offset * 1)
            Dim p3 = contours(j + offset * 2)
            Dim p4 = contours(j + offset * 3)

            Dim v1 = task.pointCloud(rect).Get(Of cv.Point3f)(p1.Y, p1.X)
            Dim v2 = task.pointCloud(rect).Get(Of cv.Point3f)(p2.Y, p2.X)
            Dim v3 = task.pointCloud(rect).Get(Of cv.Point3f)(p3.Y, p3.X)
            Dim v4 = task.pointCloud(rect).Get(Of cv.Point3f)(p4.Y, p4.X)
            Dim cross1 = crossProduct(v1 - v2, v2 - v3)
            Dim cross2 = crossProduct(v1 - v4, v4 - v3)

            Dim dot = dotProduct3D(cross1, cross2)
            If dot > bestThreshold Then
                bestThreshold = dot
                Dim k = -cross1.X * v1.X - cross1.Y * v1.Y - cross1.Z * v1.Z
                eq = New cv.Vec4f(cross1.X, cross1.Y, cross1.Z, k)
            End If
        Next
        Return eq
    End Function
    ' Based on: http://www.ilikebigbits.com/blog/2015/3/2/plane-from-points  compute plane equation from the supplied points.
    Public Function fitDepthPlane(fitDepth As List(Of cv.Point3f)) As cv.Vec4f
        Dim wDepth = New cv.Mat(fitDepth.Count, 1, cv.MatType.CV_32FC3, fitDepth.ToArray)
        Dim columnSum = wDepth.Sum()
        Dim count = CDbl(fitDepth.Count)
        Dim plane As New cv.Vec4f
        Dim centroid = New cv.Point3f
        If count > 0 Then
            centroid = New cv.Point3f(columnSum(0) / count, columnSum(1) / count, columnSum(2) / count)
            wDepth = wDepth.Subtract(centroid)
            Dim xx As Double, xy As Double, xz As Double, yy As Double, yz As Double, zz As Double
            For i = 0 To wDepth.Rows - 1
                Dim tmp = wDepth.Get(Of cv.Point3f)(i, 0)
                xx += tmp.X * tmp.X
                xy += tmp.X * tmp.Y
                xz += tmp.X * tmp.Z
                yy += tmp.Y * tmp.Y
                yz += tmp.Y * tmp.Z
                zz += tmp.Z * tmp.Z
            Next

            Dim det_x = yy * zz - yz * yz
            Dim det_y = xx * zz - xz * xz
            Dim det_z = xx * yy - xy * xy

            Dim det_max = Math.Max(det_x, det_y)
            det_max = Math.Max(det_max, det_z)

            If det_max = det_x Then
                plane(0) = 1
                plane(1) = (xz * yz - xy * zz) / det_x
                plane(2) = (xy * yz - xz * yy) / det_x
            ElseIf det_max = det_y Then
                plane(0) = (yz * xz - xy * zz) / det_y
                plane(1) = 1
                plane(2) = (xy * xz - yz * xx) / det_y
            Else
                plane(0) = (yz * xy - xz * yy) / det_z
                plane(1) = (xz * xy - yz * xx) / det_z
                plane(2) = 1
            End If
        End If

        Dim magnitude = Math.Sqrt(plane(0) * plane(0) + plane(1) * plane(1) + plane(2) * plane(2))
        Dim normal = New cv.Point3f(plane(0) / magnitude, plane(1) / magnitude, plane(2) / magnitude)
        Return New cv.Vec4f(normal.X, normal.Y, normal.Z, -(normal.X * centroid.X + normal.Y * centroid.Y + normal.Z * centroid.Z))
    End Function
    Public Function expandRect(r As cv.Rect) As cv.Rect
        Dim pad = 5
        r = New cv.Rect(r.X - pad, r.Y - pad, r.Width + pad * 2, r.Height + pad * 2)
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        Return r
    End Function
    ' http://james-ramsden.com/calculate-the-cross-product-c-code/
    Public Function crossProduct(v1 As cv.Point3f, v2 As cv.Point3f) As cv.Point3f
        Dim product As New cv.Point3f
        product.X = v1.Y * v2.Z - v1.Z * v2.Y
        product.Y = v1.Z * v2.X - v1.X * v2.Z
        product.Z = v1.X * v2.Y - v1.Y * v2.X

        If (Single.IsNaN(product.X) Or Single.IsNaN(product.Y) Or Single.IsNaN(product.Z)) Then Return New cv.Point3f(0, 0, 0)
        Dim magnitude = Math.Sqrt(product.X * product.X + product.Y * product.Y + product.Z * product.Z)
        If magnitude = 0 Then Return New cv.Point3f(0, 0, 0)
        Return New cv.Point3f(product.X / magnitude, product.Y / magnitude, product.Z / magnitude)
    End Function
    Public Function dotProduct3D(v1 As cv.Point3f, v2 As cv.Point3f) As Single
        Return Math.Abs(v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z)
    End Function
    Public Function getWorldCoordinates(p As cv.Point3f) As cv.Point3f
        Dim x = (p.X - task.calibData.ppx) / task.calibData.fx
        Dim y = (p.Y - task.calibData.ppy) / task.calibData.fy
        Return New cv.Point3f(x * p.Z, y * p.Z, p.Z)
    End Function
    Public Function getWorldCoordinatesD6(p As cv.Point3f) As cv.Vec6f
        Dim x = CSng((p.X - task.calibData.ppx) / task.calibData.fx)
        Dim y = CSng((p.Y - task.calibData.ppy) / task.calibData.fy)
        Return New cv.Vec6f(x * p.Z, y * p.Z, p.Z, p.X, p.Y, 0)
    End Function
    Public Sub drawFatLine(p1 As cv.Point2f, p2 As cv.Point2f, dst As cv.Mat, fatColor As cv.Scalar)
        Dim pad = 2
        If task.workingRes.Width >= 640 Then pad = 6
        dst.Line(p1, p2, fatColor, task.lineWidth + pad, task.lineType)
        dst.Line(p1, p2, cv.Scalar.Black, task.lineWidth, task.lineType)
    End Sub
    Public Sub sampleDrawRect(input As cv.Mat)
        If task.drawRect.Width > 0 Then
            Dim tmp = input(task.drawRect)
            Dim samples(tmp.Total * tmp.ElemSize - 1) As Byte
            Marshal.Copy(tmp.Data, samples, 0, samples.Length)
        End If
    End Sub
    Public Function randomCellColor() As cv.Vec3b
        Static msRNG As New System.Random
        Return New cv.Vec3b(msRNG.Next(50, 240), msRNG.Next(50, 240), msRNG.Next(50, 240)) ' trying to avoid extreme colors... 
    End Function
    Public Function gMatrixToStr(gMatrix As cv.Mat) As String
        Dim outStr = "Gravity transform matrix - identity matrix if gravity transform is off." + vbCrLf
        For i = 0 To gMatrix.Rows - 1
            For j = 0 To gMatrix.Cols - 1
                outStr += Format(gMatrix.Get(Of Single)(j, i), fmt3) + vbTab
            Next
            outStr += vbCrLf
        Next

        Return outStr
    End Function
    Public Sub setPointCloudGrid()
        gOptions.GridSize.Value = 8
        If task.workingRes.Width = 640 Then
            gOptions.GridSize.Value = 16
        ElseIf task.workingRes.Width = 1280 Then
            gOptions.GridSize.Value = 32
        End If
    End Sub
    Public Function separateMasks(rc As rcData, lrc As rcData) As cv.Mat
        Dim x = Math.Min(rc.rect.X, lrc.rect.X)
        Dim y = Math.Min(rc.rect.Y, lrc.rect.Y)
        Dim w = Math.Max(rc.rect.X + rc.rect.Width, lrc.rect.X + lrc.rect.Width)
        Dim h = Math.Max(rc.rect.Y + rc.rect.Y, lrc.rect.Y + lrc.rect.Y)
        Dim rect = New cv.Rect(x, y, w, h)
        Return rc.mask
    End Function
    Public Sub drawPolkaDot(pt As cv.Point2f, dst As cv.Mat)
        dst.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
        dst.Circle(pt, task.dotSize, cv.Scalar.Black, -1, task.lineType)
    End Sub
    Public Sub showSelectedCell(dst As cv.Mat)
        Dim rc = task.rc
        dst(rc.rect).SetTo(cv.Scalar.White, rc.mask)
        dst.Circle(rc.maxDist, task.dotSize, cv.Scalar.Black, -1, task.lineType)

        dst.Circle(rc.maxDStable, task.dotSize + 2, cv.Scalar.Black, -1, task.lineType)
        dst.Circle(rc.maxDStable, task.dotSize, cv.Scalar.White, -1, task.lineType)
    End Sub
    Public Sub setSelectedContour(ByRef redCells As List(Of rcData), ByRef cellMap As cv.Mat)
        If redCells.Count = 0 Then Exit Sub
        task.rc = New rcData
        Dim index = cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        If task.mouseClickFlag Then
            task.rc = redCells(index)
        Else
            If task.clickPoint = New cv.Point(0, 0) Or index >= redCells.Count Then
                If redCells.Count > 1 Then
                    task.clickPoint = redCells(1).maxDist
                    task.rc = redCells(1)
                End If
            Else
                task.rc = redCells(index)
            End If
        End If
    End Sub
    Public Function contourBuild(mask As cv.Mat, approxMode As cv.ContourApproximationModes) As List(Of cv.Point)
        Dim allContours As cv.Point()()
        cv.Cv2.FindContours(mask, allContours, Nothing, cv.RetrievalModes.External, approxMode)

        Dim maxCount As Integer, maxIndex As Integer
        For i = 0 To allContours.Count - 1
            Dim len = CInt(allContours(i).Count)
            If len > maxCount Then
                maxCount = len
                maxIndex = i
            End If
        Next
        If allContours.Count > 0 Then Return New List(Of cv.Point)(allContours(maxIndex).ToList)
        Return New List(Of cv.Point)
    End Function
    Public Function distanceN(vec1 As List(Of Single), vec2 As List(Of Single)) As Double
        Dim accum As Double
        For i = 0 To vec1.Count - 1
            accum += (vec1(i) - vec2(i)) * (vec1(i) - vec2(i))
        Next
        Return Math.Sqrt(accum)
    End Function
    Public Function distanceN(vec1() As Single, vec2() As Single) As Double
        Dim accum As Double
        For i = 0 To vec1.Count - 1
            accum += (vec1(i) - vec2(i)) * (vec1(i) - vec2(i))
        Next
        Return Math.Sqrt(accum)
    End Function
    Public Sub vbDrawContour(ByRef dst As cv.Mat, contour As List(Of cv.Point), color As cv.Scalar, Optional lineWidth As Integer = -10)
        If lineWidth = -10 Then lineWidth = task.lineWidth ' VB.Net only allows constants for optional parameter.
        If contour.Count < 3 Then Exit Sub ' this is not enough to draw.
        Dim listOfPoints = New List(Of List(Of cv.Point))
        listOfPoints.Add(contour)
        cv.Cv2.DrawContours(dst, listOfPoints, -1, color, lineWidth, task.lineType)
    End Sub
    Public Function vecToScalar(vec As cv.Vec3b) As cv.Scalar
        Return New cv.Scalar(vec(0), vec(1), vec(2))
    End Function
    Public Sub quarterBeat()
        Static quarter(4) As Boolean
        task.quarterBeat = False
        task.midHeartBeat = False
        task.heartBeat = False
        Dim ms = (task.msWatch - task.msLast) / 1000
        For i = 0 To quarter.Count - 1
            If quarter(i) = False And ms > Choose(i + 1, 0.25, 0.5, 0.75, 1.0) Then
                task.quarterBeat = True
                If i = 1 Then task.midHeartBeat = True
                If i = 3 Then task.heartBeat = True
                quarter(i) = True
            End If
        Next
        If task.heartBeat Then ReDim quarter(4)
    End Sub
    Public Function vec3fAdd(v1 As cv.Vec3f, v2 As cv.Vec3f) As cv.Vec3f
        Return New cv.Vec3f(v1(0) + v2(0), v1(1) + v2(1), v1(2) + v2(2))
    End Function
    Public Function vec3fToString(v As cv.Vec3f) As String
        Return Format(v(0), fmt3) + vbTab + Format(v(1), fmt3) + vbTab + Format(v(2), fmt3)
    End Function
    Public Function point3fToString(v As cv.Point3f) As String
        Return Format(v.X, fmt3) + vbTab + Format(v.Y, fmt3) + vbTab + Format(v.Z, fmt3)
    End Function
    Public Function vbFloat2Int(ptList2f As List(Of cv.Point2f)) As List(Of cv.Point)
        Dim ptList As New List(Of cv.Point)
        For Each pt In ptList2f
            ptList.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
        Next
        Return ptList
    End Function
    Public Sub vbDrawFPoly(ByRef dst As cv.Mat, poly As List(Of cv.Point2f), color As cv.Scalar)
        Dim minMod = Math.Min(poly.Count, task.polyCount)
        For i = 0 To minMod - 1
            dst.Line(poly(i), poly((i + 1) Mod minMod), color, task.lineWidth, task.lineType)
        Next
    End Sub
    Public Function vbFormatEquation(eq As cv.Vec4f) As String
        Dim s1 = If(eq(1) < 0, " - ", " +")
        Dim s2 = If(eq(2) < 0, " - ", " +")
        Return If(eq(0) < 0, "-", " ") + Format(Math.Abs(eq(0)), fmt3) + "*x " + s1 +
                                         Format(Math.Abs(eq(1)), fmt3) + "*y " + s2 +
                                         Format(Math.Abs(eq(2)), fmt3) + "*z = " +
                                         Format(eq(3), fmt3) + vbCrLf
    End Function
    Public Function convertVec3bToScalar(vec As cv.Vec3b) As cv.Scalar
        Return New cv.Scalar(vec(0), vec(1), vec(2))
    End Function
    Public Function vbGetMaxDist(ByRef rc As rcData) As cv.Point
        Dim mask = rc.mask.Clone
        mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
        Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
        Dim mm As mmData = vbMinMax(distance32f)
        mm.maxLoc.X += rc.rect.X
        mm.maxLoc.Y += rc.rect.Y

        Return mm.maxLoc
    End Function
    Public Function vbContourToRect(contour As List(Of cv.Point)) As cv.Rect
        Dim minX As Integer = Integer.MaxValue, minY As Integer = Integer.MaxValue, maxX As Integer, maxY As Integer
        For Each pt In contour
            If minX > pt.X Then minX = pt.X
            If minY > pt.Y Then minY = pt.Y
            If maxX < pt.X Then maxX = pt.X
            If maxY < pt.Y Then maxY = pt.Y
        Next
        Return New cv.Rect(minX, minY, maxX - minX, maxY - minY)
    End Function
    Public Function vbGetMaxIntersect(mask As cv.Mat) As cv.Point
        Dim maxRow As Integer, maxRowCount As Integer, maxCol As Integer, maxColCount As Integer
        For y = 0 To mask.Rows - 1
            Dim count = mask.Row(y).CountNonZero
            If count > maxRowCount Then
                maxRow = y
                maxRowCount = count
            End If
        Next

        For x = 0 To mask.Cols - 1
            Dim count = mask.Col(x).CountNonZero
            If count > maxColCount Then
                maxCol = x
                maxColCount = count
            End If
        Next

        If maxColCount > maxRowCount Then
            maxRow = maxColCount / 2
        Else
            maxCol = maxRowCount / 2
        End If

        ' double-check to make sure this point is inside the mask.
        For x = maxCol To mask.Cols - 1
            Dim val = mask.Get(Of Byte)(maxRow, maxCol)
            If val > 0 Then
                maxCol = x
                Exit For
            End If
        Next
        Return New cv.Point(maxCol, maxRow)
    End Function
    Public Function vbHullCenter(hull As List(Of cv.Point)) As cv.Point
        Dim ptX As New List(Of Integer), ptY As New List(Of Integer)
        For Each pt In hull
            ptX.Add(pt.X)
            ptY.Add(pt.Y)
        Next
        Return New cv.Point(ptX.Average, ptY.Average)
    End Function
    Public Function hullStr(hull As List(Of cv.Point)) As String
        Dim str As String = "(" + CStr(hull.Count) + " points) "
        For Each pt In hull
            str += pt.ToString + " "
        Next
        Return str
    End Function
    Public Function getCentroid(mask As cv.Mat, rect As cv.Rect) As cv.Point
        Dim m = cv.Cv2.Moments(mask, True)
        If m.M00 <> 0 Then Return New cv.Point(CInt(m.M10 / m.M00 + rect.X), CInt(m.M01 / m.M00 + rect.Y))
        Return New cv.Point
    End Function
    Public Function getCentroid(mask As cv.Mat) As cv.Point
        Dim m = cv.Cv2.Moments(mask, True)
        If m.M00 <> 0 Then Return New cv.Point(CInt(m.M10 / m.M00), CInt(m.M01 / m.M00))
        Return New cv.Point
    End Function
    Public Function distance3D(p1 As cv.Point3f, p2 As cv.Point3f) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Function distance3D(p1 As cv.Vec3b, p2 As cv.Vec3b) As Single
        Return Math.Sqrt((CInt(p1.Item0) - CInt(p2.Item0)) * (CInt(p1.Item0) - CInt(p2.Item0)) + (CInt(p1.Item1) - CInt(p2.Item1)) * (CInt(p1.Item1) - CInt(p2.Item1)) +
                         (CInt(p1.Item2) - CInt(p2.Item2)) * (CInt(p1.Item2) - CInt(p2.Item2)))
    End Function
    Public Function distance3D(p1 As cv.Point3i, p2 As cv.Point3i) As Single
        Return Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y) + (p1.Z - p2.Z) * (p1.Z - p2.Z))
    End Function
    Public Function distance3D(p1 As cv.Scalar, p2 As cv.Scalar) As Single
        Return Math.Sqrt((p1(0) - p2(0)) * (p1(0) - p2(0)) +
                         (p1(1) - p2(1)) * (p1(1) - p2(1)) +
                         (p1(2) - p2(2)) * (p1(2) - p2(2)))
    End Function
    Public Function vbNormalize32f(Input As cv.Mat) As cv.Mat
        Dim outMat = Input.Normalize(0, 255, cv.NormTypes.MinMax)
        If Input.Channels = 1 Then
            outMat.ConvertTo(outMat, cv.MatType.CV_8U)
            Return outMat.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If
        outMat.ConvertTo(outMat, cv.MatType.CV_8UC3)
        Return outMat
    End Function
    Public Function MakeSureImage8uC3(ByVal input As cv.Mat) As cv.Mat
        Dim outMat As New cv.Mat
        If input.Type = cv.MatType.CV_8UC3 Then
            outMat = input.Clone
            Return outMat
        End If
        If input.Type = cv.MatType.CV_32F Then
            outMat = vbNormalize32f(input)
        ElseIf input.Type = cv.MatType.CV_32SC1 Then
            input.ConvertTo(outMat, cv.MatType.CV_32F)
            outMat = vbNormalize32f(outMat)
        ElseIf input.Type = cv.MatType.CV_32SC3 Then
            input.ConvertTo(outMat, cv.MatType.CV_32F)
            outMat = outMat.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            outMat = vbNormalize32f(outMat)
        ElseIf input.Type = cv.MatType.CV_32FC3 Then
            Dim split = input.Split()
            split(0) = split(0).ConvertScaleAbs(255)
            split(1) = split(1).ConvertScaleAbs(255)
            split(2) = split(2).ConvertScaleAbs(255)
            cv.Cv2.Merge(split, outMat)
        Else
            outMat = input.Clone
        End If
        If input.Channels = 1 And input.Type = cv.MatType.CV_8UC1 Then outMat = input.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        Return outMat
    End Function
    Public nearYellow As New cv.Vec3b(255, 0, 0)
    Public farBlue As New cv.Vec3b(0, 255, 255)
    Public Function vbNearFar(factor As Single) As cv.Vec3b
        If Single.IsNaN(factor) Then Return New cv.Vec3b
        If factor > 1 Then factor = 1
        If factor < 0 Then factor = 0
        Return New cv.Vec3b(((1 - factor) * farBlue(0) + factor * nearYellow(0)),
                            ((1 - factor) * farBlue(1) + factor * nearYellow(1)),
                            ((1 - factor) * farBlue(2) + factor * nearYellow(2)))
    End Function
    Public Sub vbAddAdvice(advice As String)
        If task.advice.StartsWith("No advice for ") Then task.advice = ""
        Dim split = advice.Split(":")
        If task.advice.Contains(split(0) + ":") Then Return
        task.advice += advice + vbCrLf + vbCrLf
    End Sub
    Public Function vbMinMax(mat As cv.Mat, Optional mask As cv.Mat = Nothing) As mmData
        Dim mm As mmData
        If mask Is Nothing Then
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc)
        Else
            mat.MinMaxLoc(mm.minVal, mm.maxVal, mm.minLoc, mm.maxLoc, mask)
        End If
        Return mm
    End Function
    Public Function vblowResize(input As cv.Mat) As cv.Mat
        Return input.Resize(task.lowRes, 0, 0, cv.InterpolationFlags.Nearest)
    End Function
    Public Function vbPalette(input As cv.Mat) As cv.Mat
        task.palette.Run(input)
        Return task.palette.dst2.Clone
    End Function
    Public Function vbIntersectTest(p1 As cv.Point2f, p2 As cv.Point2f, p3 As cv.Point2f, p4 As cv.Point2f, rect As cv.Rect) As cv.Point2f
        Dim x = p3 - p1
        Dim d1 = p2 - p1
        Dim d2 = p4 - p3
        Dim cross = d1.X * d2.Y - d1.Y * d2.X
        If Math.Abs(cross) < 0.000001 Then Return New cv.Point2f
        Dim t1 = (x.X * d2.Y - x.Y * d2.X) / cross
        Dim pt = p1 + d1 * t1
        ' If pt.X >= rect.Width Or pt.Y >= rect.Height Then Return New cv.Point2f
        Return pt
    End Function
End Module







Public Structure mmData
    Dim minVal As Double
    Dim maxVal As Double
    Dim minLoc As cv.Point
    Dim maxLoc As cv.Point
End Structure





Public Structure tCell
    Dim template As cv.Mat
    Dim searchRect As cv.Rect
    Dim rect As cv.Rect
    Dim center As cv.Point2f
    Dim correlation As Single
    Dim depth As Single
    Dim strOut As String
End Structure





Public Structure gravityLine
    Dim pt1 As cv.Point3f
    Dim pt2 As cv.Point3f
    Dim len3D As Single
    Dim imageAngle As Single
    Dim arcX As Single
    Dim arcY As Single
    Dim arcZ As Single
    Dim tc1 As tCell
    Dim tc2 As tCell
End Structure





Public Structure DNAentry
    Dim color As Byte
    Dim pt As cv.Point
    Dim size As Single
    Dim rotation As Single
    Dim brushNumber As Integer
End Structure





Public Class linePoints
    Public p1 As cv.Point2f
    Public p2 As cv.Point2f
    Public slope As Single
    Public yIntercept As Single
    Public Const verticalSlope As Single = 1000000
    Sub New(_p1 As cv.Point2f, _p2 As cv.Point2f)
        p1 = _p1
        p2 = _p2

        slope = If((p1.X <> p2.X), (p1.Y - p2.Y) / (p1.X - p2.X), verticalSlope)
        yIntercept = p1.Y - slope * p1.X
    End Sub
    Sub New()
        p1 = New cv.Point2f()
        p2 = New cv.Point2f()
    End Sub
    Public Function compare(mp As linePoints) As Boolean
        If mp.p1.X = p1.X And mp.p1.Y = p1.Y And mp.p2.X = p2.X And p2.Y = p2.Y Then Return True
        Return False
    End Function
End Class








Public Structure cPoints
    Dim p1 As cv.Point
    Dim p2 As cv.Point
    Dim p3 As cv.Point
    Dim p4 As cv.Point
End Structure








Public Structure matchRect
    Dim p1 As cv.Point
    Dim p2 As cv.Point
    Dim correlation1 As Single
    Dim correlation2 As Single
End Structure




Public Structure mlData
    Dim row As Single
    Dim col As Single
    Dim red As Single
    Dim green As Single
    Dim blue As Single
End Structure






Public Class roiData
    Public depth As Single
    Public color As cv.Vec3b
End Class







Public Class fPolyData
    Public prevPoly As New List(Of cv.Point2f)
    Public lengthPrevious As New List(Of Single)
    Public polyPrevSideIndex As Integer

    Public rotateCenter As cv.Point2f
    Public rotateAngle As Single
    Public centerShift As cv.Point2f
    Public currPoly As New List(Of cv.Point2f)
    Public currLength As New List(Of Single)
    Dim jitterCheck As cv.Mat
    Dim lastJitterPixels As Integer
    Public featureLineChanged As Boolean
    Sub New()
        prevPoly = New List(Of cv.Point2f)
        currPoly = New List(Of cv.Point2f)
        polyPrevSideIndex = 0
    End Sub
    Sub New(_currPoly As List(Of cv.Point2f))
        prevPoly = New List(Of cv.Point2f)(_currPoly)
        currPoly = New List(Of cv.Point2f)(_currPoly)
        polyPrevSideIndex = 0
    End Sub
    Public Function computeCurrLengths() As Single
        currLength = New List(Of Single)
        Dim polymp = currmp()
        Dim d = polymp.p1.DistanceTo(polymp.p2)
        For i = 0 To currPoly.Count - 1
            d = currPoly(i).DistanceTo(currPoly((i + 1) Mod task.polyCount))
            currLength.Add(d)
        Next
        If lengthPrevious Is Nothing Then lengthPrevious = New List(Of Single)(currLength)
        Return d
    End Function
    Public Function computeFLineLength() As Single
        Return Math.Abs(currLength(polyPrevSideIndex) - lengthPrevious(polyPrevSideIndex))
    End Function
    Public Sub resync()
        lengthPrevious = New List(Of Single)(currLength)
        polyPrevSideIndex = lengthPrevious.IndexOf(lengthPrevious.Max)
        prevPoly = New List(Of cv.Point2f)(currPoly)
        jitterCheck.SetTo(0)
    End Sub
    Public Function prevmp() As linePoints
        Return New linePoints(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Function currmp() As linePoints
        If polyPrevSideIndex >= currPoly.Count - 1 Then polyPrevSideIndex = 0
        Return New linePoints(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Sub drawPolys(dst As cv.Mat, currPoly As List(Of cv.Point2f))
        vbDrawFPoly(dst, prevPoly, cv.Scalar.White)
        vbDrawFPoly(dst, currPoly, cv.Scalar.Yellow)
        drawFatLine(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount), dst, cv.Scalar.Yellow)
        drawFatLine(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount), dst, cv.Scalar.White)
    End Sub
    Public Sub jitterTest(dst As cv.Mat) ' return true if there is nothing to change
        If jitterCheck Is Nothing Then jitterCheck = New cv.Mat(dst.Size, cv.MatType.CV_8U, 0)
        Dim polymp = currmp()
        jitterCheck.Line(polymp.p1, polymp.p2, 255, task.lineWidth, task.lineType)
        Dim jitterPixels = jitterCheck.CountNonZero
        If jitterPixels = lastJitterPixels Then featureLineChanged = True Else featureLineChanged = False
        lastJitterPixels = jitterPixels
    End Sub
End Class





Public Structure vec5f
    Dim f1 As Single
    Dim f2 As Single
    Dim f3 As Single
    Dim f4 As Single
    Dim f5 As Single
    Public Sub New(_f1 As Single, _f2 As Single, _f3 As Single, _f4 As Single, _f5 As Single)
        f1 = _f1
        f2 = _f2
        f3 = _f3
        f4 = _f4
        f5 = _f5
    End Sub
End Structure





Public Structure vec8f
    Dim f1 As Single
    Dim f2 As Single
    Dim f3 As Single
    Dim f4 As Single
    Dim f5 As Single
    Dim f6 As Single
    Dim f7 As Single
    Dim f8 As Single
    Public Sub New(_f1 As Single, _f2 As Single, _f3 As Single, _f4 As Single, _f5 As Single, _f6 As Single, _f7 As Single, _f8 As Single)
        f1 = _f1
        f2 = _f2
        f3 = _f3
        f4 = _f4
        f5 = _f5
        f6 = _f6
        f7 = _f7
        f8 = _f8
    End Sub
End Structure






Public Class rcData
    Public rect As cv.Rect
    Public mask As cv.Mat
    Public depthMask As cv.Mat

    Public pixels As Integer
    Public depthPixels As Integer

    Public color As New cv.Vec3b
    Public colorMean As New cv.Scalar
    Public colorStdev As New cv.Scalar

    Public depthMean As cv.Point3f
    Public depthStdev As cv.Point3f

    Public minVec As cv.Point3f
    Public maxVec As cv.Point3f

    Public mmX As mmData
    Public mmY As mmData
    Public mmZ As mmData

    Public maxDist As cv.Point
    Public maxDStable As cv.Point ' keep maxDist the same if it is still on the cell.

    Public index As Integer
    Public indexLast As Integer
    Public matchCount As Integer
    Public matchFlag As Boolean
    Public nabs As New List(Of Integer)

    Public contour As New List(Of cv.Point)
    Public corners As New List(Of cv.Point)
    Public contour3D As New List(Of cv.Point3f)
    Public hull As New List(Of cv.Point)

    Public motionFlag As Boolean
    Public histogram As cv.Mat
    Public histList As List(Of Single)

    Public floodPoint As cv.Point
    Public depthCell As Boolean ' true if cell has depth.

    Public eq As cv.Vec4f ' plane equation
    Public pcaVec As cv.Vec3f
    Public Sub New()
        index = 0
        mask = New cv.Mat(1, 1, cv.MatType.CV_8U)
        rect = New cv.Rect(0, 0, 1, 1)
        depthCell = True
    End Sub
End Class




Public Class rangeData
    Public index As Integer
    Public pixels As Integer
    Public start As Integer
    Public ending As Integer
    Public Sub New(_index As Integer, _start As Integer, _ending As Integer, _pixels As Integer)
        index = _index
        pixels = _pixels
        start = _start
        ending = _ending
    End Sub
End Class