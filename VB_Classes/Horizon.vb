Imports cv = OpenCvSharp
Public Class Horizon_Basics : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create a mask of the vertical and horizontal orientation of the camera"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        dst1 = task.pcSplit(1).InRange(-0.05, 0.05)
        dst0.SetTo(0)
        dst0.SetTo(cv.Scalar.White, dst1)
        dst0.SetTo(0, task.noDepthMask)
        lines.Run(dst0)

        dst2.SetTo(0)
        If lines.lpList.Count > 0 Then
            Dim distances As New SortedList(Of Single, pointPair)(New compareAllowIdenticalSingleInverted)
            For Each lp In lines.lpList
                distances.Add(lp.p1.DistanceTo(lp.p2), lp)
            Next

            Dim lpBest = distances.ElementAt(0).Value
            Dim p1 = New cv.Point2f(0, lpBest.yIntercept)
            Dim p2 = New cv.Point2f(dst2.Width, lpBest.slope * dst2.Width + lpBest.yIntercept)
            dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
            labels(2) = "Horizontal slope/intercept = " + Format(lpBest.slope, fmt1) + "/" + Format(lpBest.yIntercept, fmt1)
        End If

        dst1 = task.pcSplit(0).InRange(-0.01, 0.01)
        dst0.SetTo(0)
        dst0.SetTo(cv.Scalar.White, dst1)
        dst0.SetTo(0, task.noDepthMask)
        lines.Run(dst0)

        If lines.lpList.Count > 0 Then
            Dim distances As New SortedList(Of Single, pointPair)(New compareAllowIdenticalSingleInverted)
            For Each lp In lines.lpList
                distances.Add(lp.p1.DistanceTo(lp.p2), lp)
            Next

            Dim lpBest = distances.ElementAt(0).Value
            Dim p1 = New cv.Point2f(0, lpBest.yIntercept)
            Dim p2 = New cv.Point2f(dst2.Width, lpBest.slope * dst2.Width + lpBest.yIntercept)
            dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
            labels(3) = "Vertical slope/intercept = " + Format(lpBest.slope, fmt1) + "/" + Format(lpBest.yIntercept, fmt1)
        End If
    End Sub
End Class
