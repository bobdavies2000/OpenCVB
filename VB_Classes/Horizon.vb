Imports cv = OpenCvSharp
Public Class Horizon_Basics : Inherits VB_Algorithm
    Public vertical As pointPair
    Public horizontal As pointPair
    Public Sub New()
        redOptions.YRangeSlider.Value = 3
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create a mask of the vertical and horizontal orientation of the camera"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If gOptions.gravityPointCloud.Checked = False Then
            vertical = New pointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            horizontal = New pointPair(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
            Exit Sub
        End If

        Dim xRatio = dst0.Width / task.quarterRes.Width
        Dim yRatio = dst0.Height / task.quarterRes.Height
        Dim splitX = task.pcSplit(0)
        Dim splitY = task.pcSplit(1)
        Dim noDepth = task.noDepthMask
        If splitX.Size <> task.quarterRes Then
            splitX = splitX.Resize(task.quarterRes, cv.InterpolationFlags.Nearest)
            splitY = splitY.Resize(task.quarterRes, cv.InterpolationFlags.Nearest)
            noDepth = task.noDepthMask.Resize(task.quarterRes, cv.InterpolationFlags.Nearest)
        End If

        dst3 = splitY.InRange(-0.05, 0.05)
        dst3.SetTo(0, noDepth)
        Dim pointsMat = dst3.FindNonZero()

        dst2.SetTo(0)
        If pointsMat.Rows > 0 Then
            Dim xVals As New List(Of Integer)
            Dim points As New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                xVals.Add(pt.X)
                points.Add(New cv.Point2f(pt.X * xRatio, pt.Y * yRatio))
            Next

            Dim p1 = points(xVals.IndexOf(xVals.Min()))
            Dim p2 = points(xVals.IndexOf(xVals.Max()))

            Dim lp = New pointPair(p1, p2)
            horizontal = lp.edgeToEdgeLine(dst2.Size)
            'horizontal = New pointPair(New cv.Point2f(0, lp.yIntercept), New cv.Point2f(dst2.Width, lp.slope * dst2.Width + lp.yIntercept))
        End If


        dst3 = splitX.InRange(-0.01, 0.01)
        dst3.SetTo(0, noDepth)
        pointsMat = dst3.FindNonZero()

        If pointsMat.Rows > 0 Then
            Dim yVals As New List(Of Integer)
            Dim points As New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                yVals.Add(pt.Y)
                points.Add(New cv.Point2f(pt.X * xRatio, pt.Y * yRatio))
            Next

            Dim p1 = points(yVals.IndexOf(yVals.Min()))
            Dim p2 = points(yVals.IndexOf(yVals.Max()))
            If Math.Abs(p1.X - p2.X) < 2 Then
                vertical = New pointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            Else
                Dim lp = New pointPair(p1, p2)
                vertical = lp.edgeToEdgeLine(dst2.Size)
                'vertical = New pointPair(New cv.Point2f(0, lp.yIntercept), New cv.Point2f(dst2.Width, lp.slope * dst2.Width + lp.yIntercept))
            End If
        End If

        dst2.Line(horizontal.p1, horizontal.p2, 255, task.lineWidth, task.lineType)
        dst2.Line(vertical.p1, vertical.p2, 255, task.lineWidth, task.lineType)
    End Sub
End Class







Public Class Horizon_UnstableResults : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public vertical As pointPair
    Public horizontal As pointPair
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
            horizontal = New pointPair(p1, p2)
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
            vertical = New pointPair(p1, p2)
            dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
            labels(3) = "Vertical slope/intercept = " + Format(lpBest.slope, fmt1) + "/" + Format(lpBest.yIntercept, fmt1)
        End If
    End Sub
End Class