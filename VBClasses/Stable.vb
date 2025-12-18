Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Stable_Basics : Inherits TaskParent
        Public lp As lpData
        Public lpLast As lpData
        Public Sub New()
            desc = "Use task.lineLongest to find the angle needed to stabilize the image."
        End Sub
        Public Function GetAngleBetweenLinesBySlopes(ByVal slope1 As Double, ByVal slope2 As Double) As Double
            Const EPSILON As Double = 0.000000001

            ' --- Handle Vertical Lines (Infinite Slope) ---
            Dim isSlope1Vertical As Boolean = Double.IsInfinity(slope1)
            Dim isSlope2Vertical As Boolean = Double.IsInfinity(slope2)

            If isSlope1Vertical AndAlso isSlope2Vertical Then
                ' Both lines are vertical, so they are parallel.
                Return 0.0 ' Angle is 0 degrees
            ElseIf isSlope1Vertical Then
                ' Line 1 is vertical (angle 90 degrees).
                ' Angle of line 2 is Atan(slope2).
                Dim angle2Degrees As Double = Math.Atan(slope2) * 180 / cv.Cv2.PI
                Dim angleDiff As Double = Math.Abs(90.0 - angle2Degrees)
                Return angleDiff
            ElseIf isSlope2Vertical Then
                ' Line 2 is vertical (angle 90 degrees).
                ' Angle of line 1 is Atan(slope1).
                Dim angle1Degrees As Double = Math.Atan(slope1) * 180 / cv.Cv2.PI
                Dim angleDiff As Double = Math.Abs(90.0 - angle1Degrees)
                Return angleDiff
            End If

            ' --- Handle Perpendicular Lines (Product of slopes is -1) ---
            ' Check if 1 + m1*m2 is very close to zero, indicating perpendicularity.
            If Math.Abs(1 + slope1 * slope2) < EPSILON Then
                Return 90.0 ' Lines are perpendicular (90 degrees)
            End If

            ' --- General Case: Use the tangent formula ---
            Dim tanTheta As Double = (slope2 - slope1) / (1 + slope1 * slope2)
            Dim angleRadians As Double = Math.Atan(tanTheta) ' Result is in (-PI/2, PI/2)
            Dim angleDegrees As Double = angleRadians * 180 / cv.Cv2.PI

            Return angleDegrees
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If standalone Then lp = task.lineLongest
            If lpLast Is Nothing Then lpLast = lp

            Dim rotateAngle = GetAngleBetweenLinesBySlopes(lp.slope, lpLast.slope)
            If rotateAngle <> 0 Then
                Dim rotateCenter = Line_Intersection.IntersectTest(lp, lpLast)
                Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
                dst2 = src.WarpAffine(M, src.Size(), cv.InterpolationFlags.Cubic)
                lpLast = lp
            Else
                If task.heartBeat Then dst2 = src.Clone
            End If

            If task.heartBeat Then lpLast = lp

            labels(2) = "Image after rotation by " + Format(rotateAngle, fmt3) + " degrees.  Move camera to see impact."
        End Sub
    End Class







    Public Class Stable_BasicsCount : Inherits TaskParent
        Public basics As New FCS_StablePoints
        Public goodCounts As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        Dim bPoint As New BrickPoint_Basics
        Public Sub New()
            desc = "Track the stable good features found in the BGR image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.features.Count > 0 Then
                basics.facetGen.inputPoints = New List(Of cv.Point2f)(task.features)
            Else
                bPoint.Run(src)
                basics.facetGen.inputPoints.Clear()
                For Each pt In bPoint.ptList
                    basics.facetGen.inputPoints.Add(New cv.Point2f(pt.X, pt.Y))
                Next
            End If
            basics.Run(src)
            dst2 = basics.dst2
            dst3 = basics.dst3

            goodCounts.Clear()
            Dim g As Integer
            For i = 0 To basics.ptList.Count - 1
                Dim pt = basics.ptList(i)
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
                g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                goodCounts.Add(g, i)
                SetTrueText(CStr(g), pt)
            Next

            labels(2) = CStr(basics.ptList.Count) + " good features stable"
        End Sub
    End Class







    Public Class Stable_Lines : Inherits TaskParent
        Public basics As New FCS_StablePoints
        Public Sub New()
            If standalone Then task.gOptions.displayDst1.Checked = True
            desc = "Track the line end points found in the BGR image and keep those that are stable."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            basics.facetGen.inputPoints.Clear()
            dst1 = src.Clone
            For Each lp In task.lines.lpList
                basics.facetGen.inputPoints.Add(lp.p1)
                basics.facetGen.inputPoints.Add(lp.p2)
                vbc.DrawLine(dst1, lp.p1, lp.p2, task.highlight)
            Next
            basics.Run(src)
            dst2 = basics.dst2
            dst3 = basics.dst3
            For Each pt In basics.ptList
                DrawCircle(dst2, pt, task.DotSize + 1, task.highlight)
                If standaloneTest() Then
                    Dim g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                    SetTrueText(CStr(g), pt)
                End If
            Next
            labels(2) = basics.labels(2)
            labels(3) = CStr(task.lines.lpList.Count) + " line end points were found and " + CStr(basics.ptList.Count) + " were stable"
        End Sub
    End Class








    Public Class Stable_FAST : Inherits TaskParent
        Public basics As New FCS_StablePoints
        Dim fast As New Corners_Basics
        Public Sub New()
            desc = "Track the FAST feature points found in the BGR image and track those that appear stable."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fast.Run(src)

            basics.facetGen.inputPoints.Clear()
            basics.facetGen.inputPoints = New List(Of cv.Point2f)(task.features)
            basics.Run(src)
            dst3 = basics.dst3
            dst2 = basics.dst2
            For Each pt In basics.ptList
                DrawCircle(dst2, pt, task.DotSize + 1, task.highlight)
                If standaloneTest() Then
                    Dim g = basics.facetGen.dst0.Get(Of Integer)(pt.Y, pt.X)
                    SetTrueText(CStr(g), pt)
                End If
            Next
            labels(2) = basics.labels(2)
            labels(3) = CStr(task.features.Count) + " features were found and " + CStr(basics.ptList.Count) + " were stable"
        End Sub
    End Class

End Namespace