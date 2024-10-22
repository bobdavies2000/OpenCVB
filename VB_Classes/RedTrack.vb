Imports cvb = OpenCvSharp
Public Class RedTrack_Basics : Inherits TaskParent
    Dim stats As New Cell_Basics
    Public redC As New RedCloud_Basics
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        If New cvb.Size(task.dst2.Width, task.dst2.Height) <> New cvb.Size(168, 94) Then task.frameHistoryCount = 1
        desc = "Get stats on each RedCloud cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        redC.Run(src)
        stats.Run(src)
        labels = stats.labels
        dst2.SetTo(0)
        For Each rc As rcData In task.redCells
            DrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
            If rc.index = task.rc.index Then DrawContour(dst2(rc.rect), rc.contour, cvb.Scalar.White, -1)
        Next
        strOut = stats.strOut
        SetTrueText(strOut, 3)
    End Sub
End Class







Public Class RedTrack_Lines : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim track As New RedTrack_Basics
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Identify and track the lines in an image as RedCloud Cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)

        If task.heartBeat Or task.motionFlag Then dst3.SetTo(0)
        Dim index As Integer
        For Each lp In lines.lpList
            DrawLine(dst3, lp.p1, lp.p2, 255)
            index += 1
            If index > 10 Then Exit For
        Next

        track.Run(dst3.Clone)
        dst0 = track.redC.dst0
        dst1 = track.redC.dst1
        dst2 = track.dst2
    End Sub
End Class










Public Class RedTrack_LineSingle : Inherits TaskParent
    Dim track As New RedTrack_Basics
    Dim leftMost As Integer, rightmost As Integer
    Dim leftCenter As cvb.Point, rightCenter As cvb.Point
    Public Sub New()
        desc = "Create a line between the rightmost and leftmost good feature to show camera motion"
    End Sub
    Private Function findNearest(pt As cvb.Point) As Integer
        Dim bestDistance As Single = Single.MaxValue
        Dim bestIndex As Integer
        For Each rc In task.redCells
            Dim d = pt.DistanceTo(rc.maxDist)
            If d < bestDistance Then
                bestDistance = d
                bestIndex = rc.index
            End If
        Next
        Return bestIndex
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        track.Run(src)
        dst0 = track.redC.dst0
        dst1 = track.redC.dst1
        dst2 = track.dst2
        If task.redCells.Count = 0 Then
            SetTrueText("No lines found to track.", 3)
            Exit Sub
        End If
        Dim xList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each rc In task.redCells
            If rc.index = 0 Then Continue For
            xList.Add(rc.rect.X, rc.index)
        Next

        Dim minLeft As Integer = xList.Count / 4
        Dim minRight As Integer = (xList.Count - minLeft)

        If leftMost = 0 Or rightmost = 0 Or leftMost = rightmost Then
            leftCenter = rightCenter ' force iteration...
            Dim iterations As Integer
            While leftCenter.DistanceTo(rightCenter) < dst2.Width / 4
                leftMost = msRNG.Next(minLeft, minRight)
                rightmost = msRNG.Next(minLeft, minRight)
                leftCenter = task.redCells(leftMost).maxDist
                rightCenter = task.redCells(rightmost).maxDist
                iterations += 1
                If iterations > 10 Then Exit Sub
            End While
        End If

        leftMost = findNearest(leftCenter)
        leftCenter = task.redCells(leftMost).maxDist

        rightmost = findNearest(rightCenter)
        rightCenter = task.redCells(rightmost).maxDist

        DrawLine(dst2, leftCenter, rightCenter, cvb.Scalar.White)
        labels(2) = track.redC.labels(2)
    End Sub
End Class







Public Class RedTrack_FeaturesKNN : Inherits TaskParent
    Public knn As New KNN_Basics
    Public feat As New Feature_Stable
    Public Sub New()
        labels = {"", "", "Output of Feature_Stable", "Grid of points to measure motion."}
        desc = "Use KNN with the good features in the image to create a grid of points"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst2 = feat.dst2

        knn.queries = New List(Of cvb.Point2f)(task.features)
        knn.Run(empty)

        dst3 = src.Clone
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.neighbors(i)(knn.neighbors(i).Count - 1)
            Dim p2 = knn.trainInput(index)
            DrawCircle(dst3, p1, task.DotSize, cvb.Scalar.Yellow)
            DrawCircle(dst3, p2, task.DotSize, cvb.Scalar.Yellow)
            DrawLine(dst3, p1, p2, cvb.Scalar.White)
        Next
        knn.trainInput = New List(Of cvb.Point2f)(knn.queries)
    End Sub
End Class








Public Class RedTrack_GoodCell : Inherits TaskParent
    Dim good As New RedTrack_GoodCellInput
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        FindSlider("Feature Sample Size").Value = 100
        desc = "Track the cells that have good features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2

        good.Run(src)
        dst3.SetTo(0)
        For Each pt In good.featureList
            DrawCircle(dst3,pt, task.DotSize, cvb.Scalar.White)
        Next
    End Sub
End Class








Public Class RedTrack_GoodCells : Inherits TaskParent
    Dim good As New RedTrack_GoodCellInput
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        desc = "Track the cells that have good features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2.Clone

        good.Run(src)
        dst3.SetTo(0)
        dst0 = src
        Dim trackCells As New List(Of rcData)
        Dim trackIndex As New List(Of Integer)
        For Each pt In good.featureList
            Dim index = task.cellMap.Get(Of Byte)(pt.Y, pt.X)
            If trackIndex.Contains(index) = False Then
                Dim rc = task.redCells(index)
                If rc.hull Is Nothing Then Continue For
                DrawContour(dst2(rc.rect), rc.hull, cvb.Scalar.White, -1)
                trackIndex.Add(index)

                DrawCircle(dst0, pt, task.DotSize, task.HighlightColor)
                DrawCircle(dst3,pt, task.DotSize, cvb.Scalar.White)
                trackCells.Add(rc)
            End If
        Next

        labels(3) = "There were " + CStr(trackCells.Count) + " cells that could be tracked."
    End Sub
End Class






Public Class RedTrack_GoodCellInput : Inherits TaskParent
    Public knn As New KNN_Basics
    Public feat As New Feature_Stable
    Public featureList As New List(Of cvb.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max feature travel distance", 0, 100, 10)
        desc = "Use KNN to find good features to track"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static distSlider = FindSlider("Max feature travel distance")
        Dim maxDistance = distSlider.Value

        feat.Run(src)
        dst2 = feat.dst2

        knn.queries = New List(Of cvb.Point2f)(task.features)
        knn.Run(empty)

        featureList.Clear()
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.neighbors(i)(0) ' find nearest
            Dim p2 = knn.trainInput(index)
            If p1.DistanceTo(p2) < maxDistance Then featureList.Add(p1)
        Next
        knn.trainInput = New List(Of cvb.Point2f)(knn.queries)
    End Sub
End Class







Public Class RedTrack_Points : Inherits TaskParent
    Dim lines As New Line_Basics
    Dim track As New RedTrack_Basics
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "", "RedCloudX_Track output", "Input to RedCloudX_Track"}
        desc = "Identify and track the end points of lines in an image of RedCloud Cells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)

        dst3.SetTo(0)
        Dim index As Integer
        For Each lp In lines.lpList
            DrawCircle(dst3, lp.p1, task.DotSize, 255)
            DrawCircle(dst3, lp.p2, task.DotSize, 255)
            index += 1
            If index >= 10 Then Exit For
        Next

        track.Run(dst3)
        dst0 = track.redC.dst0
        dst1 = track.redC.dst1
        dst2 = track.dst2
    End Sub
End Class







Public Class RedTrack_Features : Inherits TaskParent
    Dim options As New Options_Flood
    Dim feat As New Feature_Stable
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels = {"", "", "Output of Feature_Stable - input to RedCloud",
                  "Value Is correlation of x to y in contour points (0 indicates circular.)"}
        desc = "Similar to RedTrack_KNNPoints"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)

        If task.heartBeat Then dst2.SetTo(0)
        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, 255)
        Next

        redC.Run(dst2)
        dst3.SetTo(0)
        For Each rc In task.redCells
            If rc.rect.X = 0 And rc.rect.Y = 0 Then Continue For
            DrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
            If rc.contour.Count > 0 Then SetTrueText(shapeCorrelation(rc.contour).ToString(fmt3), New cvb.Point(rc.rect.X, rc.rect.Y), 3)
        Next
        SetTrueText("Move camera to see the value of this algorithm", 2)
        SetTrueText("Values are correlation of x to y.  Leans left (negative) or right (positive) or circular (neutral correlation.)", 3)
    End Sub
End Class