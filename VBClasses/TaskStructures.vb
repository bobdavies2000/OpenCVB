Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Module Structures
        Public Structure mmData
            Dim minVal As Double
            Dim maxVal As Double
            Dim minLoc As cv.Point
            Dim maxLoc As cv.Point
            Dim range As Double
        End Structure





        Public Structure DNAentry
            Dim color As Byte
            Dim pt As cv.Point
            Dim size As Single
            Dim rotation As Single
            Dim brushNumber As Integer
        End Structure






        Public Enum gifTypes
            gifdst0 = 0
            gifdst1 = 1
            gifdst2 = 2
            gifdst3 = 3
            openCVBwindow = 4
            openGLwindow = 5
            EntireScreen = 6
        End Enum







        Public Class fpData ' feature cv.Point -  excessive - trim this to fcsData...
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
                strOut += "Depth = " + depth.ToString(fmt1) + vbCrLf
                strOut += "Correlation = " + correlation.ToString(fmt1) + vbCrLf
                Return strOut
            End Function
            Sub New()
            End Sub
        End Class





        Public Class keyData
            Public mask As New cv.Mat
            Public maxDist As cv.Point
            Public rect As New cv.Rect(0, 0, 1, 1)
            Public index As Integer
            Public pixels As Integer
            Public contour As List(Of cv.Point)
            Public Shared Function buildRect(tour As cv.Point()) As cv.Rect
                Dim minX As Single = tour.Min(Function(p) p.X)
                Dim maxX As Single = tour.Max(Function(p) p.X)
                Dim minY As Single = tour.Min(Function(p) p.Y)
                Dim maxY As Single = tour.Max(Function(p) p.Y)
                Return ValidateRect(New cv.Rect(minX, minY, maxX - minX, maxY - minY))
            End Function
            Public Shared Function GetMaxDistContour(ByRef contour As keyData) As cv.Point
                Dim mask = contour.mask.Clone
                Rectangle(mask, New cv.Rect(0, 0, mask.Width, mask.Height), cv.Scalar.All(0), 1)
                Dim distance32f As New cv.Mat
                DistanceTransform(mask, distance32f, cv.DistanceTypes.L1, cv.DistanceTransformMasks.Precise, cv.MatType.CV_32F)
                Dim mm As mmData = GetMinMax(distance32f)
                mm.maxLoc.X += contour.rect.X
                mm.maxLoc.Y += contour.rect.Y
                Return mm.maxLoc
            End Function
            Public Sub New()
            End Sub
        End Class






        Public Class lpData
            Implements IEquatable(Of lpData)

            ''' <summary>Endpoint tolerance for Equals / operator = (pixels).</summary>
            Private Const pointEps As Single = 0.001F

            Public age As Integer = 1
            Public angle As Single ' varies from -90 to 90 degrees
            Public color As cv.Scalar

            Public fLessID As Integer

            Public index As Integer
            Public rightImage As Boolean ' if true, the line came from the right image.
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
            Public Shared Function AngleAtPoint(pVertex As cv.Point2f, p1 As cv.Point2f, p2 As cv.Point2f) As Double
                ' Build the two vectors that meet at the vertex
                Dim v1 As New cv.Point2f(p1.X - pVertex.X, p1.Y - pVertex.Y)
                Dim v2 As New cv.Point2f(p2.X - pVertex.X, p2.Y - pVertex.Y)

                ' Dot product
                Dim dot As Double = v1.X * v2.X + v1.Y * v2.Y

                ' Magnitudes
                Dim mag1 As Double = Math.Sqrt(v1.X * v1.X + v1.Y * v1.Y)
                Dim mag2 As Double = Math.Sqrt(v2.X * v2.X + v2.Y * v2.Y)

                ' Protect against division by zero
                If mag1 = 0 OrElse mag2 = 0 Then Return 0

                ' Compute cosine of the angle
                Dim cosTheta As Double = dot / (mag1 * mag2)

                ' Clamp due to floating-point noise
                If cosTheta > 1 Then cosTheta = 1
                If cosTheta < -1 Then cosTheta = -1

                ' Convert to degrees
                Return Math.Acos(cosTheta) * (180.0 / Math.PI)
            End Function
            Sub New(_p1 As cv.Point2f, _p2 As cv.Point2f)
                p1 = validatePoint(_p1)
                p2 = validatePoint(_p2)

                ' trying a simple convention: p1 is leftmost cv.Point
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

                If task.pcSplit IsNot Nothing Then
                    pVec1 = task.pointCloud.Get(Of cv.Vec3f)(p1.Y, p1.X)
                    If Single.IsNaN(pVec1(0)) Or pVec1(2) = 0 Then
                        Dim r = task.gridRects(p1GridIndex)
                        pVec1 = New cv.Vec3f(0, 0, Mean(task.pcSplit(2)(r), task.depthmask(r)).Item(0))
                    End If

                    pVec2 = task.pointCloud.Get(Of cv.Vec3f)(p2.Y, p2.X)
                    If Single.IsNaN(pVec2(0)) Or pVec2(2) = 0 Then
                        Dim p2GridIndex = task.gridMap.Get(Of Integer)(p2.Y, p2.X)
                        Dim r = task.gridRects(p2GridIndex)
                        pVec2 = New cv.Vec3f(0, 0, Mean(task.pcSplit(2)(r), task.depthmask(r)).Item(0))
                    End If
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

                Dim index1 = task.gridNabeMap.Get(Of Integer)(p1.Y, p1.X)
                Dim index2 = task.gridNabeMap.Get(Of Integer)(p2.Y, p2.X)
                Dim r1 = task.gridNabeRects(index1)
                Dim r2 = task.gridNabeRects(index2)
                rect = r1.Union(r2)
            End Sub
            Sub New()
                p1 = New cv.Point2f()
                p2 = New cv.Point2f()
            End Sub
            Private Shared Function PointsEqual(a As cv.Point2f, b As cv.Point2f) As Boolean
                Return Math.Abs(a.X - b.X) <= pointEps And Math.Abs(a.Y - b.Y) <= pointEps
            End Function

            ''' <summary>True when both lines have the same segment endpoints (p1 left, p2 right per constructor convention).</summary>
            Public Overloads Function Equals(other As lpData) As Boolean Implements IEquatable(Of lpData).Equals
                If other Is Nothing Then Return False
                If ReferenceEquals(Me, other) Then Return True
                Return PointsEqual(p1, other.p1) And PointsEqual(p2, other.p2)
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

            Public Function lpDisplay() As String
                Dim strOut = "rcList index = " + CStr(index) + vbCrLf
                strOut += "Age = " + CStr(task.lpD.age) + vbCrLf
                strOut += "Angle = " + angle.ToString(fmt1) + vbCrLf
                strOut += "Length (pixels) = " + task.lpD.length.ToString(fmt1) + " index = " + CStr(task.lpD.index) + vbCrLf

                strOut += "p1 = " + task.lpD.p1.ToString + ", p2 = " + task.lpD.p2.ToString + vbCrLf
                strOut += "ptE1 = " + task.lpD.ptE1.ToString + ", ptE2 = " + task.lpD.ptE2.ToString + vbCrLf + vbCrLf
                strOut += "Slope = " + task.lpD.slope.ToString(fmt3) + vbCrLf
                strOut += vbCrLf + "NOTE: the Y-Axis is inverted - Y increases down so slopes are inverted." + vbCrLf + vbCrLf
                Return strOut
            End Function
        End Class





        Public Class contourData
            Public age As Integer
            Public depth As Single
            Public hull As List(Of cv.Point)
            Public ID As Integer
            Public mask As New cv.Mat
            Public maxDist As cv.Point
            Public pixels As Integer
            Public contour As New List(Of cv.Point)
            Public rect As New cv.Rect(0, 0, 1, 1)
            Public Function GetMaxDistBuild() As cv.Point
                Dim maskTest = mask.Clone
                Rectangle(mask, New cv.Rect(0, 0, mask.Width, mask.Height), cv.Scalar.All(0), 1)
                Dim distance32f As New cv.Mat
                DistanceTransform(mask, distance32f, cv.DistanceTypes.L1, cv.DistanceTransformMasks.Precise, cv.MatType.CV_32F)
                Dim mm As mmData = GetMinMax(distance32f)
                mm.maxLoc.X += rect.X
                mm.maxLoc.Y += rect.Y
                Return mm.maxLoc
            End Function
            Public Sub New()
            End Sub
            Public Function displayData() As String
                Dim cDesc As String = ""
                cDesc += "ID = " + CStr(ID) + " (grid index of maxDist)" + vbCrLf
                cDesc += "Depth = " + depth.ToString(fmt1) + " m" + vbCrLf
                cDesc += "Number of pixels in the mask: " + CStr(pixels) + vbCrLf
                cDesc += "MaxDist cv.Point = " + maxDist.ToString + vbCrLf
                Return cDesc
            End Function
        End Class




        Public Class rcData
            Public age As Integer = 1
            Public contour As New List(Of cv.Point)
            Public contourApprox As New List(Of cv.Point)
            Public depth As Single
            Public hull As New List(Of cv.Point)
            Public index As Integer
            Public lpList As New List(Of Integer) ' index into task.lines.lplist
            Public mapID As Integer
            Public mask As New cv.Mat(New cv.Size(1, 1), cv.MatType.CV_8U, 0)
            Public maskApprox As New cv.Mat(New cv.Size(1, 1), cv.MatType.CV_8U, 0)
            Public maxDist As New cv.Point
            Public maxDStable As New cv.Point
            Public neighborMask As cv.Mat
            Public pixels As Integer
            Public rect As New cv.Rect(0, 0, 1, 1)
            Public Sub New()
            End Sub
            Public Sub New(_mask As cv.Mat, _rect As cv.Rect, mapID As Integer)
                rect = _rect
                If mapID >= 0 Then InRange(_mask, mapID, mapID, mask) Else mask = _mask.Clone
                maskApprox = mask.Clone
                pixels = CountNonZero(mask)
                contour = ContourBuild(mask, cv.ContourApproximationModes.ApproxSimple)
                If pixels > 0 Then
                    DrawContours(mask, {contour}, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link4)
                    Dim epsilon = 0.01 * ArcLength(contour, True)
                    contourApprox = ApproxPolyDP(contour.ToArray, epsilon, True).ToList
                    mask.SetTo(0)
                    maskApprox.SetTo(0)
                    DrawContours(mask, {contour}, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link4)
                    DrawContours(maskApprox, {contourApprox}, 0, cv.Scalar.All(255), -1, cv.LineTypes.Link4)
                End If
                pixels = CountNonZero(mask)
                maxDist = buildMaxDist(mask)
                depth = Mean(task.pcSplit(2)(rect), task.depthmask(rect))

                If contour.Count > 0 Then
                    hull = ConvexHull(contour.ToArray, True).ToList
                    neighborMask = New cv.Mat(rect.Size, cv.MatType.CV_8U, 0)
                    DrawContours(neighborMask, {hull}, 0, cv.Scalar.All(255), -1, task.lineType)
                    neighborMask.SetTo(0, mask)
                End If
            End Sub
            Public Function buildMaxDist(ByVal mask As cv.Mat) As cv.Point
                ' Rectangle is definitely needed.  Test it again with MaxDist_NoRectangle to verify that the rectangle is essential.
                Threshold(mask, mask, 0, 255, cv.ThresholdTypes.Binary)
                Rectangle(mask, New cv.Rect(0, 0, mask.Width, mask.Height), cv.Scalar.All(0), 1)
                Dim distance32f As New cv.Mat
                DistanceTransform(mask, distance32f, cv.DistanceTypes.L1, cv.DistanceTransformMasks.Precise, cv.MatType.CV_32F)
                Dim mm As mmData = GetMinMax(distance32f)
                Dim maxDist As cv.Point
                maxDist.X = mm.maxLoc.X + rect.X
                maxDist.Y = mm.maxLoc.Y + rect.Y
                maxDStable = maxDist

                Return maxDist
            End Function
            Public Function displayCell() As String
                Dim strout = ""
                strout += "age = " + CStr(age) + vbCrLf
                strout += "contour point count = " + CStr(contour.Count) + vbCrLf
                strout += "index = " + CStr(index) + vbCrLf
                strout += "mapID = " + CStr(mapID) + vbCrLf
                strout += "MaxDist = " + CStr(maxDist.X) + ", " + CStr(maxDist.Y) + vbCrLf
                strout += "MaxDStable = " + CStr(maxDStable.X) + ", " + CStr(maxDStable.Y) + vbCrLf
                strout += "Pixel count = " + CStr(pixels) + vbCrLf
                strout += "Rect: X = " + CStr(rect.X) + ", Y = " + CStr(rect.Y) + ", "
                strout += "Width = " + CStr(rect.Width) + ", height = " + CStr(rect.Height) + vbCrLf + vbCrLf
                strout += "ClickPoint = " + CStr(task.clickPoint.X) + ", " + CStr(task.clickPoint.Y) + vbCrLf

                Return strout
            End Function
        End Class
    End Module
End Namespace