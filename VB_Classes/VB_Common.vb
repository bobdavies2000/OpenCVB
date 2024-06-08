Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports System.Windows.Forms
Imports System.Drawing
Imports System.IO.Pipes
Module VB_Common
    Public Const fmt0 = "0"
    Public Const fmt1 = "0.0"
    Public Const fmt2 = "0.00"
    Public Const fmt3 = "0.000"
    Public Const fmt4 = "0.0000"
    Public Const depthListMaxCount As Integer = 10
    Public newPoint As New cv.Point
    Public task As VBtask

    Public pipeCount As Integer
    Public openGL_hwnd As IntPtr
    Public openGLPipe As NamedPipeServerStream

    Public allOptions As OptionsContainer
    Public Const RESULT_DST0 = 0 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST1 = 1 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST2 = 2 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const RESULT_DST3 = 3 ' 0=rgb 1=depth 2=dst1 3=dst2
    Public Const screenDWidth As Integer = 18
    Public Const screenDHeight As Integer = 20
    Public term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
    Public recordedData As Replay_Play

    Public algorithmTimes As New List(Of DateTime)
    Public algorithmStack As New Stack()
    Public pythonPipeIndex As Integer ' increment this for each algorithm to avoid any conflicts with other Python apps.

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
        DrawLine(dst, p1, p2, cv.Scalar.Black)
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
        Dim outStr = "Gravity transform matrix" + vbCrLf
        For i = 0 To gMatrix.Rows - 1
            For j = 0 To gMatrix.Cols - 1
                outStr += Format(gMatrix.Get(Of Single)(j, i), fmt3) + vbTab
            Next
            outStr += vbCrLf
        Next

        Return outStr
    End Function
    Public Sub setPointCloudGrid()
        task.gOptions.GridSize.Value = 8
        If task.workingRes.Width = 640 Then
            task.gOptions.GridSize.Value = 16
        ElseIf task.workingRes.Width = 1280 Then
            task.gOptions.GridSize.Value = 32
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
    Public Function vbRebuildCells(sortedCells As SortedList(Of Integer, rcData)) As cv.Mat
        task.redCells.Clear()
        task.redCells.Add(New rcData)
        For Each rc In sortedCells.Values
            rc.index = task.redCells.Count
            task.redCells.Add(rc)
            If rc.index >= 255 Then Exit For
        Next

        Return vbDisplayCells()
    End Function
    Public Function vbRebuildCells(cells As List(Of rcData)) As cv.Mat
        task.redCells.Clear()
        task.redCells.Add(New rcData)
        For Each rc In cells
            rc.index = task.redCells.Count
            task.redCells.Add(rc)
            If rc.index >= 255 Then Exit For
        Next

        Return vbDisplayCells()
    End Function
    Public Function vbDisplayCells() As cv.Mat
        Dim dst As New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        task.cellMap.SetTo(0)
        For Each rc In task.redCells
            dst(rc.rect).SetTo(If(task.redOptions.naturalColor.Checked, rc.naturalColor, rc.color), rc.mask)
            task.cellMap(rc.rect).SetTo(rc.index, rc.mask)
        Next
        Return dst
    End Function
    Public Sub setSelectedContour()
        If task.redCells.Count = 0 Then Exit Sub
        If task.clickPoint = newPoint And task.redCells.Count > 1 Then task.clickPoint = task.redCells(1).maxDist
        Dim index = task.cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        If index > 0 And index < task.redCells.Count Then
            ' task.clickPoint = task.redCells(index).maxDist
            task.rc = task.redCells(index)
        Else
            task.rc = task.redCells(0)
        End If
    End Sub
    Public Sub setSelectedContour(ByRef redCells As List(Of rcData), ByRef cellMap As cv.Mat)
        Static ptNew As New cv.Point
        If redCells.Count = 0 Then Exit Sub
        If task.clickPoint = ptNew And redCells.Count > 1 Then task.clickPoint = redCells(1).maxDist
        Dim index = cellMap.Get(Of Byte)(task.clickPoint.Y, task.clickPoint.X)
        task.rc = redCells(index)
        If index > 0 And index < task.redCells.Count Then
            ' task.clickPoint = redCells(index).maxDist
            task.rc = redCells(index)
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
    Public Function vbEdgeTest(pt As cv.Point, distance As Integer) As Boolean
        If pt.X < distance Then Return False
        If pt.Y < distance Then Return False
        If pt.X >= task.workingRes.Width - distance Then Return False
        If pt.Y >= task.workingRes.Height - distance Then Return False
        Return True
    End Function
    Public Function bgr2gray(src As cv.Mat) As cv.Mat
        If src.Channels <> 1 Then
            Static cvt As New Color8U_Basics
            cvt.Run(src)
            Return cvt.dst2
        End If
        Return src
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
            DrawLine(dst, poly(i), poly((i + 1) Mod minMod), color)
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
        If input.Type = cv.MatType.CV_8UC3 Then Return input
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
    Public Function vbPrepareDepthInput(index As Integer) As cv.Mat
        If task.gOptions.gravityPointCloud.Checked Then Return task.pcSplit(index) ' already oriented to gravity

        ' rebuild the pointcloud so it is oriented to gravity.
        Dim pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
        Dim split = pc.Split()
        Return split(index)
    End Function
    Public Function vblowResize(input As cv.Mat) As cv.Mat
        Return input.Resize(task.lowRes, 0, 0, cv.InterpolationFlags.Nearest)
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
    Public Function checkIntermediateResults() As VB_Parent
        If task.algName.StartsWith("CPP_") Then Return Nothing ' we don't currently support intermediate results for CPP_ algorithms.
        For Each obj In task.activeObjects
            If obj.traceName = task.intermediateName And task.firstPass = False Then Return obj
        Next
        Return Nothing
    End Function
    Public Function validateRect(ByVal r As cv.Rect, Optional ratio As Integer = 1) As cv.Rect
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X > task.workingRes.Width * ratio Then r.X = task.workingRes.Width * ratio - 1
        If r.Y > task.workingRes.Height * ratio Then r.Y = task.workingRes.Height * ratio - 1
        If r.X + r.Width > task.workingRes.Width * ratio Then r.Width = task.workingRes.Width * ratio - r.X
        If r.Y + r.Height > task.workingRes.Height * ratio Then r.Height = task.workingRes.Height * ratio - r.Y
        If r.Width <= 0 Then r.Width = 1 ' check again (it might have changed.)
        If r.Height <= 0 Then r.Height = 1
        If r.X = task.workingRes.Width * ratio Then r.X = r.X - 1
        If r.Y = task.workingRes.Height * ratio Then r.Y = r.Y - 1
        Return r
    End Function
    Public Function validatePreserve(ByVal r As cv.Rect) As cv.Rect
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X + r.Width >= task.workingRes.Width Then r.X = task.workingRes.Width - r.Width - 1
        If r.Y + r.Height >= task.workingRes.Height Then r.Y = task.workingRes.Height - r.Height - 1
        Return r
    End Function
    Public Sub AddPlotScale(dst As cv.Mat, minVal As Double, maxVal As Double, Optional lineCount As Integer = 3)
        ' draw a scale along the side
        Dim spacer = CInt(dst.Height / (lineCount + 1))
        Dim spaceVal = CInt((maxVal - minVal) / (lineCount + 1))
        If lineCount > 1 Then If spaceVal < 1 Then spaceVal = 1
        If spaceVal > 10 Then spaceVal += spaceVal Mod 10
        For i = 0 To lineCount
            Dim p1 = New cv.Point(0, spacer * i)
            Dim p2 = New cv.Point(dst.Width, spacer * i)
            dst.Line(p1, p2, cv.Scalar.White, task.cvFontThickness)
            Dim nextVal = (maxVal - spaceVal * i)
            Dim nextText = If(maxVal > 1000, Format(nextVal / 1000, "###,##0.0") + "k", Format(nextVal, fmt2))
            cv.Cv2.PutText(dst, nextText, p1, cv.HersheyFonts.HersheyPlain, task.cvFontSize, cv.Scalar.White,
                           task.cvFontThickness, task.lineType)
        Next
    End Sub
    Public Function findCorrelation(pts1 As cv.Mat, pts2 As cv.Mat) As Single
        Dim correlationMat As New cv.Mat
        cv.Cv2.MatchTemplate(pts1, pts2, correlationMat, cv.TemplateMatchModes.CCoeffNormed)
        Return correlationMat.Get(Of Single)(0, 0)
    End Function
    Public Sub DrawLine(dst As Mat, p1 As Point2f, p2 As Point2f, color As Scalar)
        dst.Line(p1, p2, color, task.lineWidth, task.lineType)
    End Sub
    Public Sub drawRotatedRectangle(rotatedRect As cv.RotatedRect, dst As cv.Mat, color As cv.Scalar)
        Dim vertices2f = rotatedRect.Points()
        Dim vertices(vertices2f.Length - 1) As cv.Point
        For j = 0 To vertices2f.Length - 1
            vertices(j) = New cv.Point(CInt(vertices2f(j).X), CInt(vertices2f(j).Y))
        Next
        dst.FillConvexPoly(vertices, color, task.lineType)
    End Sub
    Public Function vecToScalar(v As cv.Vec3b) As cv.Scalar
        Return New cv.Scalar(v(0), v(1), v(2))
    End Function
    Public Function FindSlider(opt As String) As TrackBar
        Try
            For Each frm In Application.OpenForms
                If frm.text.endswith(" Sliders") Then
                    For j = 0 To frm.trackbar.Count - 1
                        If frm.sLabels(j).text.startswith(opt) Then Return frm.trackbar(j)
                    Next
                End If
            Next
        Catch ex As Exception
            Console.WriteLine("FindSlider failed.  The application list of forms changed while iterating.  Not critical." + ex.Message)
        End Try
        Console.WriteLine("A slider was Not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")

        Return Nothing
    End Function
    Public Function findfrm(title As String) As Windows.Forms.Form
        For Each frm In Application.OpenForms
            If frm.text = title Then Return frm
        Next
        Return Nothing
    End Function
    Public Function findCheckBox(opt As String) As CheckBox
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" CheckBoxes") Then
                        For j = 0 To frm.Box.Count - 1
                            If frm.Box(j).text = opt Then Return frm.Box(j)
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findCheckBox failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A checkbox was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Private Function searchForms(opt As String, ByRef index As Integer)
        While 1
            Try
                For Each frm In Application.OpenForms
                    If frm.text.endswith(" Radio Buttons") Then
                        For j = 0 To frm.check.count - 1
                            If frm.check(j).text = opt Then
                                index = j
                                Return frm.check
                            End If
                        Next
                    End If
                Next
            Catch ex As Exception
                Console.WriteLine("findRadioForm failed.  The application list of forms changed while iterating.  Not critical.")
            End Try
            Application.DoEvents()
            Static retryCount As Integer
            retryCount += 1
            If retryCount >= 5 Then
                Console.WriteLine("A Radio button was not found!" + vbCrLf + vbCrLf + "Review the " + vbCrLf + vbCrLf + "'" + opt + "' request '")
                Exit While
            End If
        End While
        Return Nothing
    End Function
    Public Function findRadio(opt As String) As RadioButton
        Dim index As Integer
        Dim radio = searchForms(opt, index)
        If radio Is Nothing Then Return Nothing
        Return radio(index)
    End Function
    Public Function findRadioText(ByRef radioList As List(Of RadioButton)) As String
        For Each rad In radioList
            If rad.Checked Then Return rad.Text
        Next
        Return radioList(0).Text
    End Function
    Public Function findRadioIndex(ByRef radioList As List(Of RadioButton)) As String
        For i = 0 To radioList.Count - 1
            If radioList(i).Checked Then Return i
        Next
        Return 0
    End Function
    Public Sub updateSettings()
        task.fpsRate = If(task.frameCount < 30, 30, task.fpsRate)
        If task.myStopWatch Is Nothing Then task.myStopWatch = Stopwatch.StartNew()

        ' update the time measures
        task.msWatch = task.myStopWatch.ElapsedMilliseconds
        quarterBeat()
        If task.frameCount = 0 Then task.heartBeat = True
        Dim frameDuration = 1000 / task.fpsRate
        task.almostHeartBeat = If(task.msWatch - task.msLast + frameDuration * 1.5 > 1000, True, False)

        If (task.msWatch - task.msLast) > 1000 Then
            task.msLast = task.msWatch
            task.toggleOnOff = Not task.toggleOnOff
        End If

        If task.paused Then
            task.midHeartBeat = False
            task.almostHeartBeat = False
        End If

        task.histogramBins = task.gOptions.HistBinBar.Value
        task.lineWidth = task.gOptions.LineWidth.Value
        task.dotSize = task.gOptions.dotSizeSlider.Value

        task.maxZmeters = task.gOptions.maxDepth
        task.metersPerPixel = task.maxZmeters / task.workingRes.Height ' meters per pixel in projections - side and top.
        task.debugSyncUI = task.gOptions.debugSyncUI.Checked
    End Sub
    Public Function GetWindowImage(ByVal WindowHandle As IntPtr, ByVal rect As cv.Rect) As Bitmap
        Dim b As New Bitmap(rect.Width, rect.Height, Imaging.PixelFormat.Format24bppRgb)

        Using img As Graphics = Graphics.FromImage(b)
            Dim ImageHDC As IntPtr = img.GetHdc
            Try
                Using window As Graphics = Graphics.FromHwnd(WindowHandle)
                    Dim WindowHDC As IntPtr = window.GetHdc
                    BitBlt(ImageHDC, 0, 0, rect.Width, rect.Height, WindowHDC, rect.X, rect.Y, CopyPixelOperation.SourceCopy)
                    window.ReleaseHdc()
                End Using
                img.ReleaseHdc()
            Catch ex As Exception
                ' ignoring the error - they probably closed the OpenGL window.
            End Try
        End Using

        Return b
    End Function
    <System.Runtime.CompilerServices.Extension()>
    Public Sub SwapWith(Of T)(ByRef thisObj As T, ByRef withThisObj As T)
        Dim tempObj = thisObj
        thisObj = withThisObj
        withThisObj = tempObj
    End Sub
    Public Sub Swap(Of T)(ByRef a As T, ByRef b As T)
        Dim temp = b
        b = a
        a = temp
    End Sub
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





Public Class pointPair
    Public p1 As cv.Point2f
    Public p2 As cv.Point2f
    Public slope As Single
    Public yIntercept As Single
    Public xIntercept As Single
    Public length As Single
    Sub New(_p1 As cv.Point2f, _p2 As cv.Point2f)
        p1 = _p1
        p2 = _p2
        If CInt(p1.X) = CInt(p2.X) Then If p1.X < p2.X Then p2.X += 1 Else p1.X += 1 ' shift it so we can be sane.
        slope = (p1.Y - p2.Y) / (p1.X - p2.X)
        yIntercept = p1.Y - slope * p1.X
        length = p1.DistanceTo(p2)
    End Sub
    Sub New()
        p1 = New cv.Point2f()
        p2 = New cv.Point2f()
    End Sub
    Public Function edgeToEdgeLine(size As cv.Size) As pointPair
        Dim lp As New pointPair(p1, p2)
        lp.p1 = New cv.Point2f(0, yIntercept)
        lp.p2 = New cv.Point2f(size.Width, size.Width * slope + yIntercept)
        xIntercept = -yIntercept / slope
        If lp.p1.Y > size.Height Then
            lp.p1.X = (size.Height - yIntercept) / slope
            lp.p1.Y = size.Height
        End If
        If lp.p1.Y < 0 Then
            lp.p1.X = xIntercept
            lp.p1.Y = 0
        End If

        If lp.p2.Y > size.Height Then
            lp.p2.X = (size.Height - yIntercept) / slope
            lp.p2.Y = size.Height
        End If
        If lp.p2.Y < 0 Then
            lp.p2.X = xIntercept
            lp.p2.Y = 0
        End If

        Return lp
    End Function
    Public Function compare(mp As pointPair) As Boolean
        If mp.p1.X = p1.X And mp.p1.Y = p1.Y And mp.p2.X = p2.X And p2.Y = p2.Y Then Return True
        Return False
    End Function
End Class






Public Structure coinPoints
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
    Public Function prevmp() As pointPair
        Return New pointPair(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount))
    End Function
    Public Function currmp() As pointPair
        If polyPrevSideIndex >= currPoly.Count - 1 Then polyPrevSideIndex = 0
        Return New pointPair(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount))
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
        DrawLine(jitterCheck, polymp.p1, polymp.p2, 255)
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
    Public pixels As Integer
    Public floodPoint As cv.Point

    Public color As New cv.Vec3b
    Public naturalColor As New cv.Vec3b
    Public naturalGray As Integer
    Public exactMatch As Boolean
    Public pointMatch As Boolean

    Public depthPixels As Integer
    Public depthMask As cv.Mat
    Public depthMean As cv.Scalar
    Public depthStdev As cv.Scalar

    Public colorMean As cv.Scalar
    Public colorStdev As cv.Scalar

    Public minVec As cv.Point3f
    Public maxVec As cv.Point3f
    Public minLoc As cv.Point
    Public maxLoc As cv.Point

    Public maxDist As cv.Point
    Public maxDStable As cv.Point ' keep maxDist the same if it is still on the cell.

    Public index As Integer
    Public indexLast As Integer

    Public nab As Integer
    Public container As Integer

    Public contour As New List(Of cv.Point)
    Public motionFlag As Boolean
    Public motionPixels As Integer

    Public nearestFeature As cv.Point2f
    Public features As New List(Of cv.Point)
    Public featurePair As New List(Of pointPair)
    Public matchCandidatesSorted As New SortedList(Of Integer, Integer)
    Public matchCandidates As New List(Of Integer)

    ' transition these...
    Public nabs As New List(Of Integer)
    Public hull As New List(Of cv.Point)
    Public eq As cv.Vec4f ' plane equation
    Public contour3D As New List(Of cv.Point3f)
    Public Sub New()
        index = 0
        mask = New cv.Mat(1, 1, cv.MatType.CV_8U)
        depthMask = mask
        rect = New cv.Rect(0, 0, 1, 1)
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