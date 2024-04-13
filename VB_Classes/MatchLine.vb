Imports cv = OpenCvSharp
Public Class MatchLine_Basics : Inherits VB_Algorithm
    Public match As New Match_Basics
    Public lpInput As pointPair
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Private Function cornerFinder(pt As cv.Point2f, r As cv.Rect) As Integer
        ' return which corner the point is in numbering topleft = 0 clockwise, 1, 2, 3
        If r.TopLeft = pt Then Return 0
        If r.BottomRight = pt Then Return 2
        If r.Y = pt.Y Then Return 1
        Return 3
    End Function
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

        Static corner1 As Integer, corner2 As Integer
        If standalone Then
            If task.quarterBeat Or match.correlation < minC Then
                Static knn As New KNN_ClosestTracker

                knn.Run(src.Clone)
                Dim p1 = knn.lastPair.p1, p2 = knn.lastPair.p2
                Dim r = validateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                                 Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y)))
                match.template = src(r).Clone
                corner1 = cornerFinder(p1, r)
                corner2 = cornerFinder(p2, r)
            End If
        End If

        match.Run(src)
        If standaloneTest() And match.correlation >= minC Then
            dst3 = match.dst0.Resize(dst3.Size)
            Dim p1 = cornerToPoint(corner1, match.matchRect)
            Dim p2 = cornerToPoint(corner2, match.matchRect)
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class





Public Class MatchLine_Longest : Inherits VB_Algorithm
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public match As New Match_Basics
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Private Function cornerFinder(pt As cv.Point2f, r As cv.Rect) As Integer
        ' return which corner the point is in numbering topleft = 0 clockwise, 1, 2, 3
        If r.TopLeft = pt Then Return 0
        If r.BottomRight = pt Then Return 2
        If r.Y = pt.Y Then Return 1
        Return 3
    End Function
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
        options.RunVB()
        dst2 = src.Clone

        Static corner1 As Integer, corner2 As Integer
        If task.quarterBeat Or match.correlation < match.options.correlationThreshold Then
            knn.Run(src.Clone)
            Dim p1 = knn.lastPair.p1, p2 = knn.lastPair.p2
            Dim r = validateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y),
                                             Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y)))
            match.template = src(r).Clone
            corner1 = cornerFinder(p1, r)
            corner2 = cornerFinder(p2, r)
        End If

        match.Run(src)
        If standaloneTest() And match.correlation >= options.fOptions.correlationThreshold Then
            dst3 = match.dst0.Resize(dst3.Size)
            Dim p1 = cornerToPoint(corner1, match.matchRect)
            Dim p2 = cornerToPoint(corner2, match.matchRect)
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class





Public Class MatchLine_Horizon : Inherits VB_Algorithm
    Dim matchLine As New MatchLine_Basics
    Public Sub New()
        desc = "Find the horizon and match it using MatchTemplate."
    End Sub
    Public Sub RunVB(src As cv.Mat)
    End Sub
End Class
