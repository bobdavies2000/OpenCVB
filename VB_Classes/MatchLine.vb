Imports cv = OpenCvSharp
Public Class MatchLine_Basics : Inherits VB_Algorithm
    Public match As New Match_Basics
    Public lpInput As New pointPair
    Public lpOutput As pointPair
    Public corner1 As Integer, corner2 As Integer
    Public Sub New()
        desc = "Find and track a line in the BGR image."
    End Sub
    Private Function cornerToPoint(whichCorner As Integer, r As cv.Rect) As cv.Point2f
        Select Case whichCorner
            Case 0
                Return r.TopLeft
            Case 1
                Return New cv.Point2f(r.BottomRight.X, r.TopLeft.Y)
            Case 2
                Return r.BottomRight
        End Select
        Return New cv.Point2f(r.TopLeft.X, r.BottomRight.Y)
    End Function
    Public Sub RunVB(src As cv.Mat)
        Dim minC = match.options.correlationThreshold
        dst2 = src.Clone

        Static lpSave As New pointPair
        If task.quarterBeat Or match.correlation < minC Or lpSave.p1 <> lpInput.p1 Or lpSave.p2 <> lpInput.p2 Then
            lpSave = lpInput
            If standalone Then
                Static knn As New KNN_ClosestTracker
                knn.Run(src.Clone)
                lpInput = New pointPair(knn.lastPair.p1, knn.lastPair.p2)
            End If

            Dim r = validateRect(New cv.Rect(Math.Min(lpInput.p1.X, lpInput.p2.X), Math.Min(lpInput.p1.Y, lpInput.p2.Y),
                                             Math.Abs(lpInput.p1.X - lpInput.p2.X), Math.Abs(lpInput.p1.Y - lpInput.p2.Y)))
            match.template = src(r).Clone

            Dim p1 = New cv.Point(CInt(lpInput.p1.X), CInt(lpInput.p1.Y))
            ' Determine which corner - numbering topleft = 0 clockwise, 1, 2, 3
            If r.TopLeft.DistanceTo(p1) <= 2 Then
                corner1 = 0
                corner2 = 2
            ElseIf r.BottomRight.DistanceTo(p1) <= 2 Then
                corner1 = 2
                corner2 = 0
            ElseIf r.Y = p1.Y Then
                corner1 = 1
                corner2 = 3
            Else
                corner1 = 3
                corner2 = 1
            End If
        End If

        match.Run(src)
        If match.correlation >= minC Then
            If standaloneTest() Then dst3 = match.dst0.Resize(dst3.Size)
            Dim p1 = cornerToPoint(corner1, match.matchRect)
            Dim p2 = cornerToPoint(corner2, match.matchRect)
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth + 2, task.lineType)
            lpOutput = New pointPair(p1, p2)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class





Public Class MatchLine_Longest : Inherits VB_Algorithm
    Public knn As New KNN_ClosestTracker
    Public matchLine As New MatchLine_Basics
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        knn.Run(src.Clone)
        matchLine.lpInput = New pointPair(knn.lastPair.p1, knn.lastPair.p2)

        matchLine.Run(src)
        dst2 = matchLine.dst2
        dst2.Line(matchLine.lpOutput.p1, matchLine.lpOutput.p2, cv.Scalar.Red, task.lineWidth, task.lineType)

        labels(2) = "Longest line end points had correlation of " + Format(matchLine.match.correlation, fmt3) +
                    " with the original longest line."
    End Sub
End Class





Public Class MatchLine_Horizon : Inherits VB_Algorithm
    Dim matchLine As New MatchLine_Basics
    Public Sub New()
        desc = "Verify the horizon using MatchTemplate."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        'If matchLine.match.correlation < matchLine.match.options.correlationThreshold Then matchLine.lpInput = task.horizonVec
        If task.quarterBeat Then matchLine.lpInput = task.horizonVec
        matchLine.Run(src)
        dst2 = matchLine.dst2
        dst2.Line(task.horizonVec.p1, task.horizonVec.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        labels(2) = "MatchLine correlation = " + Format(matchLine.match.correlation, fmt3) + " - Red = current horizon, yellow is matchLine output"
    End Sub
End Class




Public Class MatchLine_Gravity : Inherits VB_Algorithm
    Dim matchLine As New MatchLine_Basics
    Public Sub New()
        desc = "Verify the gravity vector using MatchTemplate."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.quarterBeat Then matchLine.lpInput = task.gravityVec
        matchLine.Run(src)
        dst2 = matchLine.dst2
        dst2.Line(task.gravityVec.p1, task.gravityVec.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
        labels(2) = "MatchLine correlation = " + Format(matchLine.match.correlation, fmt3) +
                    " - Red = current gravity vector, yellow is matchLine output"
    End Sub
End Class
