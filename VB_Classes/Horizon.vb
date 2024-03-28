Imports cv = OpenCvSharp
Public Class Horizon_Basics : Inherits VB_Algorithm
    Public yData As cv.Mat
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Search for the transition from positive to negative to find the horizon."
    End Sub
    Private Function findTransition(startCol As Integer, stopCol As Integer, stepCol As Integer) As cv.Point2f
        Dim val As Single, lastVal As Single
        Dim ptX As New List(Of Single)
        Dim ptY As New List(Of Single)
        For x = startCol To stopCol Step stepCol
            For y = 0 To yData.Rows - 1
                lastVal = val
                val = yData.Get(Of Single)(y, x)
                If val > 0 And lastVal < 0 Then
                    ' change sub-pixel accuracy here 
                    Dim pt = New cv.Point2f(x, y + Math.Abs(val) / Math.Abs(val - lastVal))
                    ptX.Add(pt.X)
                    ptY.Add(pt.Y)
                    If ptX.Count >= gOptions.FrameHistory.Value Then Return New cv.Point2f(ptX.Average, ptY.Average)
                End If
            Next
        Next
        Return New cv.Point
    End Function
    Public Sub RunVB(src As cv.Mat)
        If gOptions.gravityPointCloud.Checked Then
            yData = task.pcSplit(1)
        Else
            Dim pc = (task.pointCloud.Reshape(1, task.pointCloud.Rows * task.pointCloud.Cols) * task.gMatrix).ToMat.Reshape(3, task.pointCloud.Rows)
            Dim split = pc.Split()
            yData = split(1)
        End If

        Dim p1 = findTransition(0, yData.Width - 1, 1)
        Dim p2 = findTransition(yData.Width - 1, 0, -1)
        Dim lp = New pointPair(p1, p2)
        task.horizonVec = lp.edgeToEdgeLine(dst2.Size)

        If p1.Y >= 1 Then
            strOut = "p1 = " + p1.ToString + vbCrLf + "p2 = " + p2.ToString + vbCrLf + "      val =  " +
                      Format(yData.Get(Of Single)(p1.Y, p1.X)) + vbCrLf + "lastVal = " + Format(yData.Get(Of Single)(p1.Y - 1, p1.X))
        End If
        setTrueText(strOut, 3)

        If standaloneTest() Then
            dst2.SetTo(0)
            dst2.Line(task.horizonVec.p1, task.horizonVec.p2, 255, task.lineWidth, task.lineType)
            dst2.Line(task.gravityVec.p1, task.gravityVec.p2, 255, task.lineWidth, task.lineType)
        End If
    End Sub
End Class







Public Class Horizon_FindNonZero : Inherits VB_Algorithm
    Public Sub New()
        redOptions.YRangeSlider.Value = 3
        If standalone Then gOptions.displayDst1.Checked = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        task.gravityVec = New pointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
        task.horizonVec = New pointPair(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
        labels = {"", "Horizon vector mask", "Crosshairs - gravityVec (vertical) and horizonVec (horizontal)", "Gravity vector mask"}
        desc = "Create lines for the gravity vector and horizon vector in the camera image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim xRatio = dst0.Width / task.quarterRes.Width
        Dim yRatio = dst0.Height / task.quarterRes.Height

        Dim pc = task.pointCloud.Resize(task.quarterRes)
        Dim split = pc.Split()
        split(2).SetTo(task.maxZmeters)
        cv.Cv2.Merge(split, pc)

        pc = (pc.Reshape(1, pc.Rows * pc.Cols) * task.gMatrix).ToMat.Reshape(3, pc.Rows)

        dst1 = split(1).InRange(-0.05, 0.05)
        Dim noDepth = task.noDepthMask.Resize(task.quarterRes)
        dst1.SetTo(0, noDepth)
        Dim pointsMat = dst1.FindNonZero()
        If pointsMat.Rows > 0 Then
            dst2.SetTo(0)
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
            task.horizonVec = lp.edgeToEdgeLine(dst2.Size)
            dst2.Line(task.horizonVec.p1, task.horizonVec.p2, 255, task.lineWidth, task.lineType)
        End If

        dst3 = split(0).InRange(-0.01, 0.01)
        dst3.SetTo(0, noDepth)
        pointsMat = dst3.FindNonZero()
        If pointsMat.Rows > 0 Then
            Dim yVals As New List(Of Integer)
            Dim points = New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                yVals.Add(pt.Y)
                points.Add(New cv.Point2f(pt.X * xRatio, pt.Y * yRatio))
            Next

            Dim p1 = points(yVals.IndexOf(yVals.Min()))
            Dim p2 = points(yVals.IndexOf(yVals.Max()))
            If Math.Abs(p1.X - p2.X) < 2 Then
                task.gravityVec = New pointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            Else
                Dim lp = New pointPair(p1, p2)
                task.gravityVec = lp.edgeToEdgeLine(dst2.Size)
            End If
            dst2.Line(task.gravityVec.p1, task.gravityVec.p2, 255, task.lineWidth, task.lineType)
        End If
    End Sub
End Class







Public Class Horizon_UnstableResults : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Create lines for the gravity vector and horizon vector in the camera image"
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
            task.horizonVec = New pointPair(p1, p2)
            dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
            labels(2) = "horizonVec slope/intercept = " + Format(lpBest.slope, fmt1) + "/" + Format(lpBest.yIntercept, fmt1)
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
            task.gravityVec = New pointPair(p1, p2)
            dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
            labels(3) = "gravityVec slope/intercept = " + Format(lpBest.slope, fmt1) + "/" + Format(lpBest.yIntercept, fmt1)
        End If
    End Sub
End Class







Public Class Horizon_FindNonZeroOld : Inherits VB_Algorithm
    Public Sub New()
        redOptions.YRangeSlider.Value = 3
        If standalone Then gOptions.displayDst1.Checked = True
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        task.gravityVec = New pointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
        task.horizonVec = New pointPair(New cv.Point2f(0, dst2.Height / 2), New cv.Point2f(dst2.Width, dst2.Height / 2))
        labels = {"", "Horizon vector mask", "Crosshairs - gravityVec (vertical) and horizonVec (horizontal)", "Gravity vector mask"}
        desc = "Create lines for the gravity vector and horizon vector in the camera image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If gOptions.gravityPointCloud.Checked = False Then Exit Sub

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

        dst1 = splitY.InRange(-0.05, 0.05)
        dst1.SetTo(0, noDepth)
        Dim pointsMat = dst1.FindNonZero()
        If pointsMat.Rows > 0 Then
            dst2.SetTo(0)
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
            task.horizonVec = lp.edgeToEdgeLine(dst2.Size)
            dst2.Line(task.horizonVec.p1, task.horizonVec.p2, 255, task.lineWidth, task.lineType)
        End If

        'If task.horizonVec.originalLength < dst2.Width / 2 And redOptions.YRangeSlider.Value < redOptions.YRangeSlider.Maximum Or pointsMat.Rows = 0 Then
        '    redOptions.YRangeSlider.Value += 1
        'End If

        dst3 = splitX.InRange(-0.01, 0.01)
        dst3.SetTo(0, noDepth)
        pointsMat = dst3.FindNonZero()
        If pointsMat.Rows > 0 Then
            Dim yVals As New List(Of Integer)
            Dim points = New List(Of cv.Point)
            For i = 0 To pointsMat.Rows - 1
                Dim pt = pointsMat.Get(Of cv.Point)(i, 0)
                yVals.Add(pt.Y)
                points.Add(New cv.Point2f(pt.X * xRatio, pt.Y * yRatio))
            Next

            Dim p1 = points(yVals.IndexOf(yVals.Min()))
            Dim p2 = points(yVals.IndexOf(yVals.Max()))
            If Math.Abs(p1.X - p2.X) < 2 Then
                task.gravityVec = New pointPair(New cv.Point2f(dst2.Width / 2, 0), New cv.Point2f(dst2.Width / 2, dst2.Height))
            Else
                Dim lp = New pointPair(p1, p2)
                task.gravityVec = lp.edgeToEdgeLine(dst2.Size)
            End If
            dst2.Line(task.gravityVec.p1, task.gravityVec.p2, 255, task.lineWidth, task.lineType)
        End If

        'If task.gravityVec.originalLength < dst2.Height / 2 And redOptions.XRangeSlider.Value < redOptions.XRangeSlider.Maximum Or pointsMat.Rows = 0 Then
        '    redOptions.XRangeSlider.Value += 1
        'End If
    End Sub
End Class