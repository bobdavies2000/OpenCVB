Imports OpenCvSharp
Imports cv = OpenCvSharp
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
            parent.jitterCheck.Line(polymp.p1, polymp.p2, 255, task.lineWidth, task.lineType)
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
        Public center As cv.Point ' center of the gRect
        Public color As cv.Scalar
        Public colorClass As Integer
        Public corners As New List(Of cv.Point3f)
        Public correlation As Single
        Public depth As Single
        Public index As Integer

        Public lRect As New cv.Rect ' Intel RealSense camera use this. They don't align left and color automatically.
        Public rRect As New cv.Rect ' The rect in the right image matching the left image rect.

        Public mm As mmData ' min and max values of the grayscale data.
        Public mmDepth As mmData ' min and max values of the depth data.

        Public rect As cv.Rect ' rectange under the cursor in the color image.
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




    Public Class keyData
        Public mask As cv.Mat
        Public maxDist As cv.Point
        Public rect As New cv.Rect(0, 0, 1, 1)
        Public index As Integer
        Public pixels As Integer
        Public contour As List(Of cv.Point)
        Public Function buildRect(tour As cv.Point()) As cv.Rect
            Dim minX As Single = tour.Min(Function(p) p.X)
            Dim maxX As Single = tour.Max(Function(p) p.X)
            Dim minY As Single = tour.Min(Function(p) p.Y)
            Dim maxY As Single = tour.Max(Function(p) p.Y)
            Return ValidateRect(New cv.Rect(minX, minY, maxX - minX, maxY - minY))
        End Function
        Public Function GetMaxDistContour(ByRef contour As keyData) As cv.Point
            Dim mask = contour.mask.Clone
            mask.Rectangle(New cv.Rect(0, 0, mask.Width, mask.Height), 0, 1)
            Dim distance32f = mask.DistanceTransform(cv.DistanceTypes.L1, 0)
            Dim mm As mmData = GetMinMax(distance32f)
            mm.maxLoc.X += contour.rect.X
            mm.maxLoc.Y += contour.rect.Y
            Return mm.maxLoc
        End Function
        Public Sub New()
        End Sub
    End Class




    Public Class rcData
        Public age As Integer = 1
        Public color As cv.Scalar
        Public colorChange As Integer ' 0 no change, 1 , 
        Public contour As List(Of cv.Point)
        Public contour3D As New List(Of cv.Point3f) ' here for compatibility.
        Public depthDelta As Single
        Public eq As cv.Vec4f ' only here for compatibility
        Public gridIndex As Integer
        Public hull As List(Of cv.Point)
        Public index As Integer
        Public indexLast As Integer ' only here for compatibility
        Public mask As cv.Mat
        Public maxDist As cv.Point
        Public maxDStable As cv.Point
        Public multiMask As Boolean ' indicates if RedWGrid found duplicate wGrid points in the rclist.
        Public nabs As New List(Of Integer) ' here for compatibility.
        Public pixels As Integer
        Public rect As cv.Rect
        Public wGrid As cv.Point3d
        Public wcMean As cv.Scalar
        Public Sub New()
        End Sub
        Public Sub New(_mask As cv.Mat, _rect As cv.Rect, _index As Integer)
            rect = _rect
            If _index >= 0 Then
                mask = _mask.InRange(_index, _index)
                index = _index
            Else
                mask = _mask.Clone
            End If
            contour = ContourBuild(mask)
            If _index >= 0 Then
                If contour.Count >= 3 Then ' need at least 3 points for a contour.
                    Dim listOfPoints = New List(Of List(Of cv.Point))({contour})
                    mask = New cv.Mat(mask.Size, cv.MatType.CV_8U, 0)
                    cv.Cv2.DrawContours(mask, listOfPoints, 0, cv.Scalar.All(index), -1, cv.LineTypes.Link4)

                    ' keep the hull points around (there aren't many of them.)
                    hull = cv.Cv2.ConvexHull(contour.ToArray, True).ToList
                End If
            End If
            buildMaxDist()

            gridIndex = task.gridMap.Get(Of Integer)(maxDist.Y, maxDist.X)
            If _index >= 0 Then color = task.scalarColors(index Mod 255)
            pixels = mask.CountNonZero
            wcMean = task.pointCloud(rect).Mean(task.depthmask(rect))
            Dim x = Math.Round(wcMean(0) * 1000 / task.reduction)
            Dim y = Math.Round(wcMean(1) * 1000 / task.reduction)
            Dim z = Math.Round(wcMean(2) * 1000 / task.reduction)
            If Math.Abs(x) < 0.000000000001 Then x = 0
            If Math.Abs(y) < 0.000000000001 Then y = 0
            If Math.Abs(z) < 0.000000000001 Then z = 0
            wGrid = New cv.Point3d(x, y, z)
            If Single.IsInfinity(wcMean(2)) Then depthDelta = 0
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
        Public Function displayCell() As String
            Dim strout = "Age = " + CStr(age) + vbCrLf
            strout += "Color = " + color.ToString + vbCrLf
            If contour IsNot Nothing Then
                strout += "Contour count = " + CStr(contour.Count) + vbCrLf
            End If
            If Single.IsNaN(depthDelta) = False Then
                strout += "DepthDelta (mm's) = " + Format(CInt(depthDelta * 1000), "00") + vbCrLf
                strout += "Hull count = " + If(hull Is Nothing, "0", CStr(hull.Count)) + vbCrLf
                strout += "index = " + CStr(index) + vbCrLf
                strout += "MaxDist = " + CStr(maxDist.X) + "," + CStr(maxDist.Y) + vbCrLf
                strout += "Multi-Mask flag = " + CStr(multiMask) + vbCrLf
                strout += "Pixel count = " + CStr(pixels) + vbCrLf
                strout += "Rect: X = " + CStr(rect.X) + ", Y = " + CStr(rect.Y) + ", "
                strout += "width = " + CStr(rect.Width) + ", height = " + CStr(rect.Height) + vbCrLf
                strout += "World Coordinates = " + Format(wcMean(0), fmt3) + " " +
                                                   Format(wcMean(1), fmt3) + " " +
                                                   Format(wcMean(2), fmt3) + vbCrLf
                strout += "World Grid coordinates = " + CStr(wGrid.X) + ", " + CStr(wGrid.Y) + vbCrLf
                strout += "ClickPoint = " + CStr(task.clickPoint.X) + ", " + CStr(task.clickPoint.Y) + vbCrLf
            Else
                strout = "The depth data for this cell is NaN. StereoLabs specific problem."
            End If

            Return strout
        End Function
    End Class





    Public Class lpData
        Implements IEquatable(Of lpData)

        ''' <summary>Endpoint tolerance for Equals / operator = (pixels).</summary>
        Private Const pointEps As Single = 0.001F

        Public age As Integer = 1
        Public angle As Single ' varies from -90 to 90 degrees
        Public color As cv.Scalar
        Public index As Integer
        Public length As Single

        Public p1 As cv.Point2f
        Public p2 As cv.Point2f

        Public pVec1 As cv.Vec3f
        Public pVec2 As cv.Vec3f
        Public ptE1 As cv.Point2f ' end points - goes to the edge of the image.
        Public ptE2 As cv.Point2f ' end points - goes to the edge of the image.
        Public ptCenter As cv.Point2f

        Public rect As cv.Rect
        Public slope As Single

        Public Shared Function validatePoint(pt As cv.Point2f) As cv.Point2f
            If CInt(pt.X) < 0 Then pt.X = 0
            If CInt(pt.X) >= task.color.Width Then pt.X = task.color.Width - 1
            If CInt(pt.Y) < 0 Then pt.Y = 0
            If CInt(pt.Y) >= task.color.Height Then pt.Y = task.color.Height - 1

            Return pt
        End Function
        Public Shared Function computeAngle(p1 As cv.Point2f, p2 As cv.Point2f) As Single
            Dim angleRadians As Double = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X)
            Dim angle = CType(angleRadians * RadToDeg, Single)
            If angle >= 90.0 Then angle -= 180.0
            If angle < -90.0 Then angle += 180.0
            Return angle
        End Function
        Public Shared Function AngleAtPoint(pVertex As Point2f, p1 As Point2f, p2 As Point2f) As Double
            ' Build the two vectors that meet at the vertex
            Dim v1 As New Point2f(p1.X - pVertex.X, p1.Y - pVertex.Y)
            Dim v2 As New Point2f(p2.X - pVertex.X, p2.Y - pVertex.Y)

            ' Dot product
            Dim dot As Double = v1.X * v2.X + v1.Y * v2.Y

            ' Magnitudes
            Dim mag1 As Double = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y)
            Dim mag2 As Double = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y)

            ' Protect against division by zero
            If mag1 = 0 OrElse mag2 = 0 Then Return 0

            ' Compute cosine of the angle
            Dim cosTheta As Double = dot / (mag1 * mag2)

            ' Clamp due to floating‑point noise
            If cosTheta > 1 Then cosTheta = 1
            If cosTheta < -1 Then cosTheta = -1

            ' Convert to degrees
            Return Math.Acos(cosTheta) * (180.0 / Math.PI)
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

            Dim p1GridIndex = task.gridMap.Get(Of Integer)(p1.Y, p1.X)
            color = task.scalarColors(p1GridIndex Mod 255)

            pVec1 = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
            If Single.IsNaN(pVec1(0)) Or pVec1(2) = 0 Then
                Dim r = task.gridRects(p1GridIndex)
                pVec1 = New cv.Vec3f(0, 0, task.pcSplit(2)(r).Mean(task.depthmask(r)).Item(0))
            End If

            pVec2 = task.pointCloud.Get(Of cv.Vec3f)(p2.Y, p2.X)
            If Single.IsNaN(pVec2(0)) Or pVec2(2) = 0 Then
                Dim p2GridIndex = task.gridMap.Get(Of Integer)(p2.Y, p2.X)
                Dim r = task.gridRects(p2GridIndex)
                pVec2 = New cv.Vec3f(0, 0, task.pcSplit(2)(r).Mean(task.depthmask(r)).Item(0))
            End If

            If p1.X <> p2.X Then
                Dim b = p1.Y - p1.X * slope
                If p1.Y = p2.Y Then
                    ptE1 = New cv.Point2f(0, p1.Y)
                    ptE2 = New cv.Point2f(task.workRes.Width - 1, p1.Y)
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
                    ptE1 = pts(0)
                    If pts.Count < 2 Then
                        If CInt(x2) >= task.workRes.Width Then pts.Add(New cv.Point2f(CInt(x2), task.workRes.Height - 1))
                        If CInt(y2) >= task.workRes.Height Then pts.Add(New cv.Point2f(task.workRes.Width - 1, CInt(y2)))
                    End If
                    ptE2 = pts(1)
                End If
            Else
                ptE1 = New cv.Point2f(p1.X, 0)
                ptE2 = New cv.Point2f(p1.X, task.workRes.Height - 1)
            End If
            If ptE1.X >= task.workRes.Width Then ptE1.X = task.workRes.Width - 1
            If ptE2.X >= task.workRes.Width Then ptE2.X = task.workRes.Width - 1
            If ptE1.Y >= task.workRes.Height Then ptE1.Y = task.workRes.Height - 1
            If ptE2.Y >= task.workRes.Height Then ptE2.Y = task.workRes.Height - 1
            ptCenter = New cv.Point2f((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2)

            If p2.X = p1.X Then
                angle = 90
                Exit Sub
            End If

            angle = computeAngle(p1, p2)

            Dim pad As Integer = 5
            Dim w = Math.Abs(p1.X - p2.X) + pad * 2
            Dim h = Math.Abs(p1.Y - p2.Y) + pad
            ' p1 is always leftmost point.
            If Math.Abs(angle) > 45 Then
                rect = New cv.Rect(p1.X - pad, Math.Min(p1.Y, p2.Y),
                                           Math.Max(pad * 2, w), Math.Max(pad * 2, h))
            Else
                rect = New cv.Rect(p1.X, Math.Min(p1.Y, p2.Y) - pad,
                                       Math.Max(pad * 2, w), Math.Max(pad * 2, h))
            End If

            rect = ValidateRect(rect)
        End Sub
        Sub New()
            p1 = New cv.Point2f()
            p2 = New cv.Point2f()
        End Sub

        Private Shared Function PointsEqual(a As cv.Point2f, b As cv.Point2f) As Boolean
            Return Math.Abs(a.X - b.X) <= pointEps AndAlso Math.Abs(a.Y - b.Y) <= pointEps
        End Function

        ''' <summary>True when both lines have the same segment endpoints (p1 left, p2 right per constructor convention).</summary>
        Public Overloads Function Equals(other As lpData) As Boolean Implements IEquatable(Of lpData).Equals
            If other Is Nothing Then Return False
            If ReferenceEquals(Me, other) Then Return True
            Return PointsEqual(p1, other.p1) AndAlso PointsEqual(p2, other.p2)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(TryCast(obj, lpData))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim h1 = CInt(Math.Round(p1.X / pointEps)) Xor (CInt(Math.Round(p1.Y / pointEps)) << 1)
            Dim h2 = CInt(Math.Round(p2.X / pointEps)) Xor (CInt(Math.Round(p2.Y / pointEps)) << 1)
            Return h1 Xor (h2 << 2)
        End Function

        Public Shared Operator =(left As lpData, right As lpData) As Boolean
            If left Is right Then Return True
            If left Is Nothing OrElse right Is Nothing Then Return False
            Return left.Equals(right)
        End Operator

        Public Shared Operator <>(left As lpData, right As lpData) As Boolean
            Return Not (left = right)
        End Operator

        Public Function lpDisplay(ByRef dst As cv.Mat) As String
            Dim strOut = "rcList index = " + CStr(index) + vbCrLf
            strOut += "Age = " + CStr(task.lpD.age) + vbCrLf
            strOut += "Angle = " + Format(angle, fmt1) + vbCrLf
            strOut += "Length (pixels) = " + Format(task.lpD.length, fmt1) + " index = " + CStr(task.lpD.index) + vbCrLf

            strOut += "p1 = " + task.lpD.p1.ToString + ", p2 = " + task.lpD.p2.ToString + vbCrLf
            strOut += "ptE1 = " + task.lpD.ptE1.ToString + ", ptE2 = " + task.lpD.ptE2.ToString + vbCrLf + vbCrLf
            strOut += "Slope = " + Format(task.lpD.slope, fmt3) + vbCrLf
            strOut += vbCrLf + "NOTE: the Y-Axis is inverted - Y increases down so slopes are inverted." + vbCrLf + vbCrLf
            Return strOut
        End Function
    End Class
End Module
