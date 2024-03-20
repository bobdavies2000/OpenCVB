Imports cv = OpenCvSharp
Public Class Horizon_Basics : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public Sub New()
        redOptions.YRangeSlider.Value = 30
        desc = "Create a mask of the surv"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        dst2.SetTo(0)
        dst1 = task.pcSplit(1).InRange(-0.05, 0.05)
        dst2.SetTo(cv.Scalar.White, dst1)

        dst1 = task.pcSplit(0).InRange(-0.01, 0.01)
        dst2.SetTo(cv.Scalar.White, dst1)

        dst2.SetTo(0, task.noDepthMask)

        lines.Run(dst2)
        Dim hSlope As New List(Of Single)
        Dim vSlope As New List(Of Single)
        Dim hCept As New List(Of Single)
        Dim vCept As New List(Of Single)
        For Each lp In lines.lpList
            If lp.yIntercept > 0 Then
                hSlope.Add(lp.slope)
                hCept.Add(lp.yIntercept)
            Else
                vSlope.Add(lp.slope)
                vCept.Add(lp.yIntercept)
            End If
        Next

        Dim horizonSlope = hSlope.Average()
        Dim horizonCept = hCept.Average()

        Dim vertSlope = vSlope.Average()
        Dim vertCept = vCept.Average()

        dst3.SetTo(0)
        Dim p1 = New cv.Point2f(0, horizonCept)
        Dim p2 = New cv.Point2f(dst2.Width, horizonSlope * dst2.Width + horizonCept)
        dst3.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)

        Dim p3 = New cv.Point2f(0, vertCept)
        Dim p4 = New cv.Point2f(dst2.Width, vertSlope * dst2.Width + vertCept)
        dst3.Line(p3, p4, task.highlightColor, task.lineWidth, task.lineType)

        labels(2) = "horizon slope/intercept = " + Format(horizonSlope, fmt1) + "/" + Format(horizonCept, fmt1)
        labels(3) = "vertical slope/intercept = " + Format(vertSlope, fmt1) + "/" + Format(vertCept, fmt1)
    End Sub
End Class
