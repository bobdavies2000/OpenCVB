Imports System.Windows.Forms.Design.AxImporter
Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Stabilizer_Basics : Inherits TaskParent
        Public Sub New()
            desc = "Use algTask.lineLongest to find the angle needed to stabilize the image."
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
            Static lpLast As lpData = algTask.lineLongest

            Dim lp = algTask.lineLongest
            If lp.pE1 = lpLast.pE1 And lp.pE2 = lpLast.pE2 Or algTask.lineLongestChanged Then
                dst2 = src
                If algTask.lineLongestChanged Then lpLast = algTask.lineLongest
            Else
                Dim rotateAngle = GetAngleBetweenLinesBySlopes(lp.slope, lpLast.slope)

                Dim rotateCenter = Line_Intersection.IntersectTest(lp, lpLast)
                Dim M = cv.Cv2.GetRotationMatrix2D(rotateCenter, -rotateAngle, 1)
                dst2 = src.WarpAffine(M, src.Size(), cv.InterpolationFlags.Cubic)

                labels(2) = "Image after rotation by " + Format(rotateAngle, fmt3) + " degrees"
            End If
        End Sub
    End Class
End Namespace