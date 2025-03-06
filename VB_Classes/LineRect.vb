Imports cv = OpenCvSharp
Public Class LineRect_Basics : Inherits TaskParent
    Public lineRectMap As New cv.Mat(dst2.Size, cv.MatType.CV_32S, 0)
    Public lpPair As New List(Of Tuple(Of linePoints, linePoints, cv.Rect))
    Public Sub New()
        labels(2) = "Use mouse to display the color and depth means of the both sides of the line."
        desc = "Analyze data on either side of a line detected in the image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        task.lines.Run(src)
        dst2 = task.lines.dst2

        dst3.SetTo(0)
        lineRectMap.SetTo(0)
        lpPair.Clear()
        lpPair.Add(New Tuple(Of linePoints, linePoints, cv.Rect)(New linePoints, New linePoints, New cv.Rect))
        For Each lp In task.lpList
            Dim lp1 As linePoints
            Dim lp2 As linePoints
            If Math.Abs(lp.slope) > 1 Then
                Dim p1 = New cv.Point(lp.p1.X - 2, lp.p1.Y)
                Dim p2 = New cv.Point(lp.p2.X - 2, lp.p2.Y)
                lp1 = New linePoints(p1, p2)
                Dim p3 = New cv.Point(lp.p1.X + 2, lp.p1.Y)
                Dim p4 = New cv.Point(lp.p2.X + 2, lp.p2.Y)
                lp2 = New linePoints(p3, p4)
            Else
                Dim p1 = New cv.Point(lp.p1.X, lp.p1.Y - 2)
                Dim p2 = New cv.Point(lp.p2.X, lp.p2.Y - 2)
                lp1 = New linePoints(p1, p2)
                Dim p3 = New cv.Point(lp.p1.X, lp.p1.Y + 2)
                Dim p4 = New cv.Point(lp.p2.X, lp.p2.Y + 2)
                lp2 = New linePoints(p3, p4)
            End If

            dst3.Line(lp1.p1, lp1.p2, cv.Scalar.Yellow, task.lineWidth, cv.LineTypes.Link4)
            dst3.Line(lp2.p1, lp2.p2, cv.Scalar.Red, task.lineWidth, cv.LineTypes.Link4)

            Dim rect = lp1.rect.Union(lp2.rect)
            lineRectMap(rect).SetTo(lpPair.Count)
            lpPair.Add(New Tuple(Of linePoints, linePoints, cv.Rect)(lp1, lp2, rect))
        Next

        Dim index = lineRectMap.Get(Of Integer)(task.mouseMovePoint.Y, task.mouseMovePoint.X)
        Dim tup = lpPair(index)
        Dim c1 = tup.Item1.colorMean
        Dim c2 = tup.Item2.colorMean
        Dim colorStr1 = Format(c1(0), fmt0) + "/" + Format(c1(1), fmt0) + "/" + Format(c1(2), fmt0)
        Dim colorStr2 = Format(c2(0), fmt0) + "/" + Format(c2(1), fmt0) + "/" + Format(c2(2), fmt0)
        labels(3) = "Color1 = " + colorStr1 + " color2 = " + colorStr2 + " and depth1 = " + Format(tup.Item1.depthMean, fmt1) +
                    "m and depth2 " + Format(tup.Item2.depthMean, fmt1) + "m"
        dst3.Rectangle(tup.Item3, cv.Scalar.White, task.lineWidth)
        task.color.Rectangle(tup.Item3, cv.Scalar.White, task.lineWidth)
    End Sub
End Class






Public Class LineRect_Internal : Inherits TaskParent
    Dim lRect As New LineRect_Basics
    Public Sub New()
        desc = "Remove lines which have similar color on both sides of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lRect.Run(src)

    End Sub
End Class


