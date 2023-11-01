Imports cv = OpenCvSharp
Public Class RedTrack_Basics : Inherits VB_Algorithm
    Dim stats As New RedBP_CellStats
    Public redC As New RedCloud_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        If task.workingRes <> New cv.Size(168, 94) Then task.historyCount = 1
        desc = "Get stats on each RedCloud cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)

        stats.Run(src)
        labels = stats.labels
        dst2.SetTo(0)
        For Each rc As rcData In task.redCells
            vbDrawContour(dst2(rc.rect), rc.contour, rc.color, -1)
            If rc.index = task.rcSelect.index Then vbDrawContour(dst2(rc.rect), rc.contour, cv.Scalar.White, -1)
        Next
        strOut = stats.strOut
        setTrueText(strOut, 3)
    End Sub
End Class







Public Class RedTrack_Lines : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Dim track As New RedTrack_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Identify and track the lines in an image as RedCloud Cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)

        If heartBeat() Or task.motionFlag Then dst3.SetTo(0)
        For i = 0 To Math.Min(lines.sortLength.Count - 1, 10)
            Dim line = lines.sortLength.ElementAt(i)
            Dim mps = lines.mpList(line.Value)
            dst3.Line(mps.p1, mps.p2, 255, task.lineWidth, task.lineType)
        Next

        track.Run(dst3.Clone)
        dst2 = track.dst2
    End Sub
End Class










Public Class RedTrack_LineSingle : Inherits VB_Algorithm
    Dim track As New RedTrack_Basics
    Public Sub New()
        desc = "Create a line between the rightmost and leftmost good feature to show camera motion"
    End Sub
    Private Function findNearest(pt As cv.Point) As Integer
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
    Public Sub RunVB(src As cv.Mat)
        track.Run(src)
        dst2 = track.dst2
        If task.redCells.Count = 0 Then
            setTrueText("No lines found to track.", 3)
            Exit Sub
        End If
        Dim xList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each rc In task.redCells
            If rc.index = 0 Then Continue For
            xList.Add(rc.rect.X, rc.index)
        Next

        Dim minLeft As Integer = xList.Count / 4
        Dim minRight As Integer = (xList.Count - minLeft)

        Static leftMost As Integer, rightmost As Integer
        Static leftCenter As cv.Point, rightCenter As cv.Point
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

        dst2.Line(leftCenter, rightCenter, cv.Scalar.White, task.lineWidth, task.lineType)
        labels(2) = track.redC.labels(2)
    End Sub
End Class







Public Class RedTrack_FeaturesKNN : Inherits VB_Algorithm
    Public knn As New KNN_Basics
    Public good As New Feature_Basics
    Public Sub New()
        labels = {"", "", "Output of Feature_Basics", "Grid of points to measure motion."}
        desc = "Use KNN with the good features in the image to create a grid of points"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        good.Run(src)
        dst2 = good.dst2

        knn.queries = New List(Of cv.Point2f)(good.corners)
        knn.Run(Nothing)

        dst3 = src.Clone
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.neighbors(i)(knn.neighbors(i).Count - 1)
            Dim p2 = knn.trainInput(index)
            dst3.Circle(p1, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            dst3.Circle(p2, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
            dst3.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class








Public Class RedTrack_GoodCell : Inherits VB_Algorithm
    Dim good As New RedTrack_GoodCellInput
    Dim hulls As New RedBP_Hulls
    Public Sub New()
        findSlider("Sample Size").Value = 100
        findSlider("Distance").Value = 3
        desc = "Track the cells that have good features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hulls.Run(src)
        dst2 = hulls.dst2

        good.Run(src)
        dst3.SetTo(0)
        For Each pt In good.featureList
            dst3.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class








Public Class RedTrack_GoodCells : Inherits VB_Algorithm
    Dim good As New RedTrack_GoodCellInput
    Dim hulls As New RedBP_Hulls
    Public Sub New()
        desc = "Track the cells that have good features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
                vbDrawContour(dst2(rc.rect), rc.hull, cv.Scalar.White, -1)
                trackIndex.Add(index)

                dst0.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                dst3.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
                trackCells.Add(rc)
            End If
        Next

        labels(3) = "There were " + CStr(trackCells.Count) + " cells that could be tracked."
    End Sub
End Class






Public Class RedTrack_GoodCellInput : Inherits VB_Algorithm
    Public knn As New KNN_Basics
    Public good As New Feature_Basics
    Public featureList As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max feature travel distance", 0, 100, 10)
        desc = "Use KNN to find good features to track"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static distSlider = findSlider("Max feature travel distance")
        Dim maxDistance = distSlider.Value

        good.Run(src)
        dst2 = good.dst2

        knn.queries = New List(Of cv.Point2f)(good.corners)
        knn.Run(Nothing)

        featureList.Clear()
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.neighbors(i)(0) ' find nearest
            Dim p2 = knn.trainInput(index)
            If p1.DistanceTo(p2) < maxDistance Then featureList.Add(p1)
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class







Public Class RedTrack_Points : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Dim track As New RedTrack_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "RedCloudX_Track output", "Input to RedCloudX_Track"}
        desc = "Identify and track the end points of lines in an image of RedCloud Cells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lines.Run(src)

        dst3.SetTo(0)
        For i = 0 To Math.Min(lines.sortLength.Count - 1, 10)
            Dim line = lines.sortLength.ElementAt(i)
            Dim mps = lines.mpList(line.Value)
            dst3.Circle(mps.p1, task.dotSize, 255, -1)
            dst3.Circle(mps.p2, task.dotSize, 255, -1)
        Next

        track.Run(dst3)
        dst2 = track.dst2
    End Sub
End Class







Public Class RedTrack_Core : Inherits VB_Algorithm
    Public redC As New RedCloud_Basics
    Public Sub New()
        labels = {"", "", "Points tracked with RedCloud", ""}
        desc = "Show feature location history"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_8U Then
            setTrueText("Input to " + traceName + " should be points, circles, lines, or rectangles")
            Exit Sub
        End If
        dst0 = Not src.Threshold(0, 255, cv.ThresholdTypes.Binary)
        redC.Run(dst0)

        dst2.SetTo(0)
        For Each rc In task.redCells
            If rc.rect.Width < dst2.Width / 2 Or rc.rect.Height < dst2.Height / 2 Then
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                dst2.Rectangle(rc.rect, rc.color, -1)
            End If
        Next
    End Sub
End Class






Public Class RedTrack_Features : Inherits VB_Algorithm
    Dim options As New Options_Flood
    Dim good As New Feature_Basics
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        labels = {"", "", "Output of Feature_Basics - input to RedCloud",
                  "Value Is correlation of x to y in contour points (0 indicates circular.)"}
        desc = "Similar to RedTrack_KNNPoints"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        good.Run(src)

        If heartBeat() Then dst2.SetTo(0)
        For Each pt In good.corners
            dst2.Circle(pt, task.dotSize, 255, -1)
        Next

        redC.Run(dst2)
        task.color = src

        dst3.SetTo(0)
        For Each rc In task.redCells
            If rc.rect.X = 0 And rc.rect.Y = 0 Then Continue For
            vbDrawContour(dst3(rc.rect), rc.contour, rc.color, -1)
            If rc.contour.Count > 0 Then setTrueText(Format(shapeCorrelation(rc.contour), fmt3), New cv.Point(rc.rect.X, rc.rect.Y), 3)
        Next
        setTrueText("Move camera to see the value of this algorithm", 2)
        setTrueText("Values are correlation of x to y.  Leans left (negative) or right (positive) or circular (neutral correlation.)", 3)
    End Sub
End Class