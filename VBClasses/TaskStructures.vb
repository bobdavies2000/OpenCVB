Imports cv = OpenCvSharp
Namespace VBClasses
    Public Module Structures
        Public Enum pointStyle
            unFiltered = 0
            filtered = 1
            flattened = 2
            flattenedAndFiltered = 3
        End Enum




        Public Structure mmData
            Dim minVal As Double
            Dim maxVal As Double
            Dim minLoc As cv.Point
            Dim maxLoc As cv.Point
            Dim range As Double
        End Structure





        Public Class tCell
            Public template As cv.Mat
            Public searchRect As cv.Rect
            Public rect As cv.Rect
            Public center As cv.Point2f
            Public correlation As Single
            Public depth As Single
            Public strOut As String
            Public Sub New()
                strOut = ""
            End Sub
        End Class





        Public Class gravityLine
            Public pt1 As cv.Point3f
            Public pt2 As cv.Point3f
            Public len3D As Single
            Public imageAngle As Single
            Public arcX As Single
            Public arcY As Single
            Public arcZ As Single
            Public tc1 As tCell
            Public tc2 As tCell
            Public Sub New()
                tc1 = New tCell
                tc2 = New tCell
            End Sub
        End Class





        Public Structure DNAentry
            Dim color As Byte
            Dim pt As cv.Point
            Dim size As Single
            Dim rotation As Single
            Dim brushNumber As Integer
        End Structure






        Public Structure coinPoints
            Dim p1 As cv.Point
            Dim p2 As cv.Point
            Dim p3 As cv.Point
            Dim p4 As cv.Point
        End Structure






        Public Structure matchRect
            Dim p1 As cv.Point
            Dim p2 As cv.Point
            Dim p1Correlation As Single
            Dim p2Correlation As Single
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
            Public Function prevmp() As lpData
                Return New lpData(prevPoly(polyPrevSideIndex), prevPoly((polyPrevSideIndex + 1) Mod task.polyCount))
            End Function
            Public Function currmp() As lpData
                If polyPrevSideIndex >= currPoly.Count - 1 Then polyPrevSideIndex = 0
                Return New lpData(currPoly(polyPrevSideIndex), currPoly((polyPrevSideIndex + 1) Mod task.polyCount))
            End Function
            Public Sub jitterTest(dst As cv.Mat, parent As Object) ' return true if there is nothing to change
                If jitterCheck Is Nothing Then jitterCheck = New cv.Mat(dst.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
                Dim polymp = currmp()
                parent.DrawLine(jitterCheck, polymp.p1, polymp.p2, 255, task.lineWidth)
                Dim jitterPixels = jitterCheck.CountNonZero
                If jitterPixels = lastJitterPixels Then featureLineChanged = True Else featureLineChanged = False
                lastJitterPixels = jitterPixels
            End Sub
        End Class






        Public Class triangleData
            Public color As cv.Point3f
            Public facets(3) As cv.Point3f
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







        Public Enum gifTypes
            gifdst0 = 0
            gifdst1 = 1
            gifdst2 = 2
            gifdst3 = 3
            openCVBwindow = 4
            openGLwindow = 5
            EntireScreen = 6
        End Enum







        Public Class fpData ' feature point -  excessive - trim this to fcsData...
            Public index As Integer
            Public age As Integer = 1
            Public ID As Single
            Public travelDistance As Single
            Public periph As Boolean
            Public facets As List(Of cv.Point)
            Public pt As cv.Point
            Public ptLast As cv.Point
            Public ptHistory As List(Of cv.Point)
            Public depth As Single
            Public brickIndex As Integer
            Sub New()
                facets = New List(Of cv.Point)
                ptHistory = New List(Of cv.Point)
            End Sub
        End Class








        Public Class brickData
            Public age As Integer = 1
            Public color As cv.Scalar
            Public correlation As Single
            Public index As Integer

            Public rect As cv.Rect ' rectange under the cursor in the color image.
            Public lRect As New cv.Rect ' Intel RealSense camera use this. They don't align left and color automatically.
            Public rRect As New cv.Rect ' The rect in the right image matching the left image rect.

            Public center As cv.Point ' center of the brick
            Public depth As Single

            Public mm As mmData ' min and max values of the depth data.
            Public corners As New List(Of cv.Point3f)
            Public colorClass As Integer
            Public Function displayCell() As String
                Dim strOut = "rcList index = " + CStr(index) + vbCrLf
                strOut += "Age = " + CStr(age) + vbCrLf
                strOut += "Rect: X = " + CStr(rect.X) + ", Y = " + CStr(rect.Y) + ", "
                strOut += ", width = " + CStr(rect.Width) + ", height = " + CStr(rect.Height) + vbCrLf
                strOut += "Depth = " + Format(depth, fmt1) + vbCrLf
                strOut += "Correlation = " + Format(correlation, fmt1) + vbCrLf
                Return strOut
            End Function
            Sub New()
            End Sub
        End Class




        Public Class maskData
            Public rect As cv.Rect
            Public mask As cv.Mat
            Public contour As New List(Of cv.Point)
            Public index As Integer
            Public maxDist As cv.Point
            Public pixels As Integer
            Public depthMean As Single
            Public mm As mmData
            Public Sub New()
                mask = New cv.Mat(1, 1, cv.MatType.CV_8U)
                rect = New cv.Rect(0, 0, 1, 1)
            End Sub
        End Class




        Public Class contourData
            Public age As Integer
            Public depth As Single
            Public hull As List(Of cv.Point)
            Public ID As Integer
            Public mask As cv.Mat
            Public maxDist As cv.Point
            Public mm As mmData
            Public pixels As Integer
            Public points As New List(Of cv.Point)
            Public rect As New cv.Rect(0, 0, 1, 1)
            Public Function buildRect(tour As cv.Point()) As cv.Rect
                Dim minX As Single = tour.Min(Function(p) p.X)
                Dim maxX As Single = tour.Max(Function(p) p.X)
                Dim minY As Single = tour.Min(Function(p) p.Y)
                Dim maxY As Single = tour.Max(Function(p) p.Y)
                Return ValidateRect(New cv.Rect(minX, minY, maxX - minX, maxY - minY))
            End Function
            Public Sub New()
            End Sub
        End Class





        Public Class lpData
            Public age As Integer
            Public angle As Single ' varies from -90 to 90 degrees

            Public color As cv.Scalar

            Public p1GridIndex As Integer
            Public p2GridIndex As Integer

            Public index As Integer
            Public indexVTop As Integer = -1
            Public indexVBot As Integer = -1
            Public indexHLeft As Integer = -1
            Public indexHRight As Integer = -1

            Public length As Single

            Public p1 As cv.Point2f
            Public p2 As cv.Point2f
            Public pVec1 As cv.Vec3f
            Public pVec2 As cv.Vec3f
            Public pE1 As cv.Point2f ' end points - goes to the edge of the image.
            Public pE2 As cv.Point2f ' end points - goes to the edge of the image.
            Public ptCenter As cv.Point2f

            Public rect As cv.Rect
            Public roRect As cv.RotatedRect
            Public slope As Single
            Public Function perpendicularPoints(pt As cv.Point2f) As lpData
                Dim perpSlope = -1 / slope
                Dim angleRadians As Double = Math.Atan(perpSlope)
                Dim xShift = task.brickSize * Math.Cos(angleRadians)
                Dim yShift = task.brickSize * Math.Sin(angleRadians)
                Dim p1 = New cv.Point(pt.X + xShift, pt.Y + yShift)
                Dim p2 = New cv.Point(pt.X - xShift, pt.Y - yShift)
                If p1.X < 0 Then p1.X = 0
                If p1.X >= task.color.Width Then p1.X = task.color.Width - 1
                If p1.Y < 0 Then p1.Y = 0
                If p1.Y >= task.color.Height Then p1.Y = task.color.Height - 1
                If p2.X < 0 Then p2.X = 0
                If p2.X >= task.color.Width Then p2.X = task.color.Width - 1
                If p2.Y < 0 Then p2.Y = 0
                If p2.Y >= task.color.Height Then p2.Y = task.color.Height - 1
                Return New lpData(p1, p2)
            End Function
            Public Sub drawRoRectMask(dst As cv.Mat)
                Dim vertices2f = roRect.Points
                Dim vertices As New List(Of cv.Point)
                For Each pt In vertices2f
                    vertices.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
                Next
                cv.Cv2.FillConvexPoly(dst, vertices, 255, cv.LineTypes.AntiAlias)
            End Sub
            Public Sub drawRoRect(dst As cv.Mat)
                Dim vertices = roRect.Points
                For i = 0 To vertices.Count - 1
                    vbc.DrawLine(dst, vertices(i), vertices((i + 1) Mod 4), task.highlight)
                Next
            End Sub
            Public Sub CalculateRotatedRectFromLine()
                Dim deltaX As Single = p2.X - p1.X
                Dim deltaY As Single = p2.Y - p1.Y
                Dim thickness As Single = 3
                Dim outSize = New cv.Size2f(length, thickness)

                Dim angleRadians As Double = Math.Atan2(deltaY, deltaX)
                angle = CType(angleRadians * (180.0 / Math.PI), Single)
                If angle >= 90.0 Then angle -= 180.0
                If angle < -90.0 Then angle += 180.0
                roRect = New cv.RotatedRect(ptCenter, outSize, angle)
                angle *= -1
                rect = ValidateRect(roRect.BoundingRect)
                If rect.Width <= 15 Then
                    rect = ValidateRect(New cv.Rect(rect.X - (20 - rect.Width) / 2, rect.Y, 20, rect.Height))
                End If
                If rect.Height <= 15 Then
                    rect = ValidateRect(New cv.Rect(rect.X, rect.Y - (20 - rect.Height) / 2, rect.Width, 20))
                End If
            End Sub
            Public Shared Function validatePoint(pt As cv.Point2f) As cv.Point2f
                If pt.X < 0 Then pt.X = 0
                If pt.X > task.color.Width - 1 Then pt.X = task.color.Width - 1
                If pt.Y < 0 Then pt.Y = 0
                If pt.Y > task.color.Height - 1 Then pt.Y = task.color.Height - 1
                Return pt
            End Function
            Sub New(_p1 As cv.Point2f, _p2 As cv.Point2f)
                p1 = validatePoint(_p1)
                p2 = validatePoint(_p2)

                ' trying a simple convention: p1 is leftmost point
                If p1.X > p2.X Then
                    Dim ptTemp = p1
                    p1 = p2
                    p2 = ptTemp
                End If

                If p1.X = p2.X Then
                    slope = (p1.Y - p2.Y) / (p1.X + 0.001 - p2.X)
                Else
                    slope = (p1.Y - p2.Y) / (p1.X - p2.X)
                End If

                length = p1.DistanceTo(p2)

                p1GridIndex = task.gridMap.Get(Of Integer)(p1.Y, p1.X)
                p2GridIndex = task.gridMap.Get(Of Integer)(p2.Y, p2.X)
                color = task.scalarColors(p1GridIndex Mod 255)

                pVec1 = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                If Single.IsNaN(pVec1(0)) Or pVec1(2) = 0 Then
                    Dim r = task.gridRects(p1GridIndex)
                    pVec1 = New cv.Vec3f(0, 0, task.pcSplit(2)(r).Mean(task.depthmask(r)).Item(0))
                End If
                pVec2 = task.pointCloud.Get(Of cv.Vec3f)(p2.Y, p2.X)
                If Single.IsNaN(pVec2(0)) Or pVec2(2) = 0 Then
                    Dim r = task.gridRects(p2GridIndex)
                    pVec2 = New cv.Vec3f(0, 0, task.pcSplit(2)(r).Mean(task.depthmask(r)).Item(0))
                End If

                If p1.X <> p2.X Then
                    Dim b = p1.Y - p1.X * slope
                    If p1.Y = p2.Y Then
                        pE1 = New cv.Point2f(0, p1.Y)
                        pE2 = New cv.Point2f(task.workRes.Width - 1, p1.Y)
                    Else
                        Dim x1 = -b / slope
                        Dim x2 = (task.workRes.Height - b) / slope
                        Dim y1 = b
                        Dim y2 = slope * task.workRes.Width + b

                        Dim pts As New List(Of cv.Point2f)
                        If x1 >= 0 And x1 <= task.workRes.Width Then pts.Add(New cv.Point2f(x1, 0))
                        If x2 >= 0 And x2 <= task.workRes.Width Then pts.Add(New cv.Point2f(x2, task.workRes.Height - 1))
                        If y1 >= 0 And y1 <= task.workRes.Height Then pts.Add(New cv.Point2f(0, y1))
                        If y2 >= 0 And y2 <= task.workRes.Height Then pts.Add(New cv.Point2f(task.workRes.Width - 1, y2))
                        pE1 = pts(0)
                        If pts.Count < 2 Then
                            If CInt(x2) >= task.workRes.Width Then pts.Add(New cv.Point2f(CInt(x2), task.workRes.Height - 1))
                            If CInt(y2) >= task.workRes.Height Then pts.Add(New cv.Point2f(task.workRes.Width - 1, CInt(y2)))
                        End If
                        pE2 = pts(1)
                    End If
                Else
                    pE1 = New cv.Point2f(p1.X, 0)
                    pE2 = New cv.Point2f(p1.X, task.workRes.Height - 1)
                End If
                ptCenter = New cv.Point2f((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)

                Dim bpRow = task.bricksPerRow - 1
                Dim bpCol = task.bricksPerCol - 1
                If pE1.Y = 0 Then indexVTop = pE1.X / task.workRes.Width * bpRow
                If pE1.Y = task.workRes.Height - 1 Then indexVBot = pE1.X / task.workRes.Width * bpRow

                If pE2.Y = 0 Then indexVTop = pE2.X / task.workRes.Width * bpRow
                If pE2.Y = task.workRes.Height - 1 Then indexVBot = pE2.X / task.workRes.Width * bpRow

                If pE1.X = 0 Then indexHLeft = pE1.Y / task.workRes.Height * bpCol
                If pE1.X = task.workRes.Width - 1 Then indexHRight = pE1.Y / task.workRes.Height * bpCol

                If pE2.X = 0 Then indexHLeft = pE2.Y / task.workRes.Height * bpCol
                If pE2.X = task.workRes.Width - 1 Then indexHRight = pE2.Y / task.workRes.Height * bpCol

                CalculateRotatedRectFromLine()
            End Sub
            Sub New()
                p1 = New cv.Point2f()
                p2 = New cv.Point2f()
            End Sub
            Public Function compare(lp As lpData) As Boolean
                If lp.p1.X = p1.X And lp.p1.Y = p1.Y And lp.p2.X = p2.X And p2.Y = p2.Y Then Return True
                Return False
            End Function
            Public Function displayCell(ByRef dst As cv.Mat) As String
                dst.SetTo(0)
                For Each lp In task.lines.lpList
                    dst.Line(lp.p1, lp.p2, white, task.lineWidth, cv.LineTypes.Link8)
                    dst.Circle(lp.ptCenter, task.DotSize, task.highlight, -1)
                Next

                dst.Line(task.lpD.p1, task.lpD.p2, task.highlight, task.lineWidth + 1, task.lineType)

                Dim strOut = "rcList index = " + CStr(index) + vbCrLf
                strOut = "Line ID = " + CStr(task.lpD.p1GridIndex) + " Age = " + CStr(task.lpD.age) + vbCrLf
                strOut += "Length (pixels) = " + Format(task.lpD.length, fmt1) + " index = " + CStr(task.lpD.index) + vbCrLf
                strOut += "p1GridIndex = " + CStr(task.lpD.p1GridIndex) + " p2GridIndex = " + CStr(task.lpD.p2GridIndex) + vbCrLf

                strOut += "p1 = " + task.lpD.p1.ToString + ", p2 = " + task.lpD.p2.ToString + vbCrLf
                strOut += "pE1 = " + task.lpD.pE1.ToString + ", pE2 = " + task.lpD.pE2.ToString + vbCrLf + vbCrLf
                strOut += "RGB Angle = " + CStr(task.lpD.angle) + vbCrLf
                strOut += "RGB Slope = " + Format(task.lpD.slope, fmt3) + vbCrLf
                strOut += vbCrLf + "NOTE: the Y-Axis is inverted - Y increases down so slopes are inverted." + vbCrLf + vbCrLf
                Return strOut
            End Function

        End Class




        Public Class oldrcData
            Public rect As cv.Rect
            Public mask As cv.Mat
            Public pixels As Integer
            Public age As Integer

            Public color As cv.Scalar

            Public depthPixels As Integer
            Public depthMask As cv.Mat
            Public depth As Single

            Public mmX As mmData
            Public mmY As mmData
            Public mmZ As mmData

            Public maxDist As cv.Point
            Public maxDStable As cv.Point ' keep maxDist the same if it is still on the cell.

            Public index As Integer
            Public indexLast As Integer

            Public container As Integer

            Public contour As New List(Of cv.Point)

            Public ptFacets As New List(Of cv.Point)
            Public ptList As New List(Of cv.Point)

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




        Public Class rcData
            Public age As Integer = 1
            Public color As cv.Scalar
            Public contour As List(Of cv.Point)
            Public depth As Single
            Public hull As List(Of cv.Point)
            Public index As Integer
            Public gridIndex As Integer
            Public mask As cv.Mat
            Public maxDist As cv.Point
            Public pixels As Integer
            Public rect As cv.Rect
            Public Sub New()
            End Sub
            Public Shared Function getHullMask(hull As List(Of cv.Point), mask As cv.Mat) As cv.Mat
                Dim hullMask = New cv.Mat(mask.Size, cv.MatType.CV_8U, 0)
                Dim listOfPoints = New List(Of List(Of cv.Point))({hull})
                cv.Cv2.DrawContours(hullMask, listOfPoints, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link8)
                Return hullMask
            End Function
            Public Sub buildMaxDist()
                Dim tmp As cv.Mat = mask.Clone
                ' Rectangle is definitely needed.  Test it again with MaxDist_NoRectangle.
                tmp.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
                Dim distance32f = tmp.DistanceTransform(cv.DistanceTypes.L1, 0)
                Dim mm As mmData = GetMinMax(distance32f)
                maxDist.X = mm.maxLoc.X + rect.X
                maxDist.Y = mm.maxLoc.Y + rect.Y
            End Sub
            Public Sub New(_mask As cv.Mat, _rect As cv.Rect, _index As Integer, Optional minContours As Integer = 3)
                rect = _rect
                mask = _mask.InRange(_index, _index)
                index = -1 ' assume it is not going to be valid...
                contour = ContourBuild(mask)
                If contour.Count >= minContours Then
                    index = _index
                    If contour.Count >= 3 Then
                        Dim listOfPoints = New List(Of List(Of cv.Point))({contour})
                        mask = New cv.Mat(mask.Size, cv.MatType.CV_8U, 0)
                        cv.Cv2.DrawContours(mask, listOfPoints, 0, cv.Scalar.All(index), -1, cv.LineTypes.Link4)

                        ' keep the hull points around (there aren't many of them.)
                        hull = cv.Cv2.ConvexHull(contour.ToArray, True).ToList
                        gridIndex = task.gridMap.Get(Of Integer)(rect.TopLeft.Y + contour(0).Y,
                                                          rect.TopLeft.X + contour(0).X) Mod 255
                    Else
                        gridIndex = task.gridMap.Get(Of Integer)(rect.TopLeft.Y, rect.TopLeft.X) Mod 255
                    End If
                    buildMaxDist()

                    color = task.vecColors(index)
                    pixels = mask.CountNonZero
                    depth = task.pcSplit(2)(rect).Mean(task.depthmask(rect))(0)
                End If
            End Sub
            Public Function displayCell() As String
                Dim strOut = "rcList index = " + CStr(index) + vbCrLf
                strOut += "Age = " + CStr(age) + vbCrLf
                strOut += "Rect: X = " + CStr(rect.X) + ", Y = " + CStr(rect.Y) + ", "
                strOut += ", width = " + CStr(rect.Width) + ", height = " + CStr(rect.Height) + vbCrLf
                strOut += "MaxDist = " + CStr(maxDist.X) + "," + CStr(maxDist.Y) + vbCrLf
                strOut += "Depth = " + Format(depth, fmt1) + vbCrLf
                strOut += "Color = " + color.ToString + vbCrLf
                strOut += "Pixel count = " + CStr(pixels) + vbCrLf
                If hull IsNot Nothing Then strOut += "Hull count = " + CStr(hull.Count) + vbCrLf
                Return strOut
            End Function
        End Class
    End Module
End Namespace