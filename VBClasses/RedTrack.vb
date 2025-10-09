Imports cv = OpenCvSharp
Public Class RedTrack_Basics : Inherits TaskParent
    Public Sub New()
        If New cv.Size(task.workRes.Width, task.workRes.Height) <> New cv.Size(168, 94) Then task.frameHistoryCount = 1
        desc = "Get stats on each RedCloud cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = runRedList(src, labels(2))
        labels(2) = task.redList.labels(2)
        dst2.SetTo(0)
        For Each rc As rcData In task.redList.rcList
            DrawTour(dst2(rc.rect), rc.contour, rc.color, -1)
            If rc.index = task.rcD.index Then DrawTour(dst2(rc.rect), rc.contour, white, -1)
        Next
    End Sub
End Class







Public Class RedTrack_Lines : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, 0)
        desc = "Identify and track the lines in an image as RedCloud Cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then dst3.SetTo(0)
        Dim index As Integer
        For Each lp In task.lines.lpList
            DrawLine(dst3, lp.p1, lp.p2, 255)
            index += 1
            If index > 10 Then Exit For
        Next

        dst2 = runRedList(dst3, labels(2))
    End Sub
End Class










Public Class RedTrack_LineSingle : Inherits TaskParent
    Dim track As New RedTrack_Basics
    Dim leftMost As Integer, rightmost As Integer
    Dim leftCenter As cv.Point, rightCenter As cv.Point
    Public Sub New()
        desc = "Create a line between the rightmost and leftmost good feature to show camera motion"
    End Sub
    Private Function findNearest(pt As cv.Point) As Integer
        Dim bestDistance As Single = Single.MaxValue
        Dim bestIndex As Integer
        For Each rc In task.redList.rcList
            Dim d = pt.DistanceTo(rc.maxDist)
            If d < bestDistance Then
                bestDistance = d
                bestIndex = rc.index
            End If
        Next
        Return bestIndex
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        track.Run(src)
        dst2 = track.dst2
        If task.redList.rcList.Count = 0 Then
            SetTrueText("No lines found to track.", 3)
            Exit Sub
        End If
        Dim xList As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each rc In task.redList.rcList
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
                leftCenter = task.redList.rcList(leftMost).maxDist
                rightCenter = task.redList.rcList(rightmost).maxDist
                iterations += 1
                If iterations > 10 Then Exit Sub
            End While
        End If

        leftMost = findNearest(leftCenter)
        leftCenter = task.redList.rcList(leftMost).maxDist

        rightmost = findNearest(rightCenter)
        rightCenter = task.redList.rcList(rightmost).maxDist

        DrawLine(dst2, leftCenter, rightCenter, white)
        labels(2) = task.redList.labels(2)
    End Sub
End Class







Public Class RedTrack_FeaturesKNN : Inherits TaskParent
    Public knn As New KNN_Basics
    Public Sub New()
        labels = {"", "", "Output of Feature_Stable", "Grid of points to measure motion."}
        desc = "Use KNN with the good features in the image to create a grid of points"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        knn.queries = New List(Of cv.Point2f)(task.features)
        knn.Run(src)

        dst3 = src.Clone
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.neighbors(i)(knn.neighbors(i).Count - 1)
            If index >= 0 And index < knn.trainInput.Count Then
                Dim p2 = knn.trainInput(index)
                DrawCircle(dst3, p1, task.DotSize, cv.Scalar.Yellow)
                DrawCircle(dst3, p2, task.DotSize, cv.Scalar.Yellow)
                DrawLine(dst3, p1, p2, white)
            End If
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class







Public Class RedTrack_GoodCellInput : Inherits TaskParent
    Public knn As New KNN_Basics
    Public featureList As New List(Of cv.Point2f)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Max feature travel distance", 0, 100, 10)
        desc = "Use KNN to find good features to track"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static distSlider = OptionParent.FindSlider("Max feature travel distance")
        Dim maxDistance = distSlider.Value

        knn.queries = New List(Of cv.Point2f)(task.features)
        knn.Run(src)

        featureList.Clear()
        For i = 0 To knn.neighbors.Count - 1
            Dim p1 = knn.queries(i)
            Dim index = knn.neighbors(i)(0) ' find nearest
            If index >= 0 And index < knn.trainInput.Count Then
                Dim p2 = knn.trainInput(index)
                If p1.DistanceTo(p2) < maxDistance Then featureList.Add(p1)
            End If
        Next
        knn.trainInput = New List(Of cv.Point2f)(knn.queries)
    End Sub
End Class







Public Class RedTrack_Points : Inherits TaskParent
    Dim track As New RedTrack_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "RedCloudX_Track output", "Input to RedCloudX_Track"}
        desc = "Identify and track the end points of lines in an image of RedCloud Cells"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3.SetTo(0)
        Dim index As Integer
        For Each lp In task.lines.lpList
            DrawCircle(dst3, lp.p1, task.DotSize, 255)
            DrawCircle(dst3, lp.p2, task.DotSize, 255)
            index += 1
            If index >= 10 Then Exit For
        Next

        track.Run(dst3)
        dst2 = track.dst2
    End Sub
End Class







Public Class RedTrack_Features : Inherits TaskParent
    Public Sub New()
        task.redList = New RedList_Basics
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels = {"", "", "Output of Feature_Stable - input to RedCloud",
                  "Value Is correlation of x to y in contour points (0 indicates circular.)"}
        desc = "Similar to RedTrack_KNNPoints"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.heartBeat Then dst2.SetTo(0)
        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, 255)
        Next

        task.redList.Run(dst2)
        dst3.SetTo(0)
        For Each rc In task.redList.rcList
            If rc.rect.X = 0 And rc.rect.Y = 0 Then Continue For
            DrawTour(dst3(rc.rect), rc.contour, rc.color, -1)
            If rc.contour.Count > 0 Then SetTrueText(RedList_ShapeCorrelation.shapeCorrelation(rc.contour).ToString(fmt3), New cv.Point(rc.rect.X, rc.rect.Y), 3)
        Next
        SetTrueText("Move camera to see the value of this algorithm", 2)
        SetTrueText("Values are correlation of x to y.  Leans left (negative) or right (positive) or circular (neutral correlation.)", 3)
    End Sub
End Class







Public Class XO_RedList_Tiers : Inherits TaskParent
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        desc = "Use the Depth_TierZ algorithm to create a color-based RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)
        dst1 = ShowPalette(binar4.dst2)

        tiers.Run(src)
        dst3 = tiers.dst3

        dst0 = tiers.dst2 + binar4.dst2
        dst2 = runRedList(dst0, labels(2))
        labels(3) = tiers.labels(2)
    End Sub
End Class




Public Class XO_RedList_TiersBinarize : Inherits TaskParent
    Dim tiers As New Depth_Tiers
    Dim binar4 As New Bin4Way_Regions
    Public Sub New()
        desc = "Use the Depth_TierZ with Bin4Way_Regions algorithm to create a color-based RedCloud"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        binar4.Run(src)

        tiers.Run(src)
        dst2 = tiers.dst2 + binar4.dst2

        dst2 = runRedList(dst2, labels(2))
    End Sub
End Class







Public Class XO_RedList_TopX : Inherits TaskParent
    Public topXcells As New List(Of cv.Point)
    Public Sub New()
        desc = "Isolate the top X cells and use the rest of the image as an input mask."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst3 = runRedList(src, labels(2))
        dst2.SetTo(0)

        If task.heartBeat Or task.optionsChanged Then
            topXcells.Clear()
            For Each rc In task.redList.rcList
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                topXcells.Add(rc.maxDist)
            Next
        Else
            Dim maxList As New List(Of cv.Point)
            For Each pt In topXcells
                Dim index = task.redList.rcMap.Get(Of Byte)(pt.Y, pt.X)
                Dim rc = task.redList.rcList(index)
                dst2(rc.rect).SetTo(rc.color, rc.mask)
                DrawCircle(dst2, rc.maxDist, task.DotSize, task.highlight)
                maxList.Add(rc.maxDist)
            Next
            topXcells = New List(Of cv.Point)(maxList)
        End If
        labels(2) = "The Top " + CStr(topXcells.Count) + " largest cells in rcList."

        dst1 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = dst1.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
        task.redList.inputRemoved = dst1
    End Sub
End Class





Public Class XO_RedList_UnmatchedCount : Inherits TaskParent
    Dim myFrameCount As Integer
    Dim changedCellCounts As New List(Of Integer)
    Dim framecounts As New List(Of Integer)
    Dim frameLoc As New List(Of cv.Point)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Count the unmatched cells and display them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        myFrameCount += 1
        If standalone Then dst2 = runRedList(src, labels(2))

        Dim unMatchedCells As Integer
        Dim mostlyColor As Integer
        For i = 0 To task.redList.rcList.Count - 1
            Dim rc = task.redList.rcList(i)
            If task.redList.rcList(i).depthPixels / task.redList.rcList(i).pixels < 0.5 Then mostlyColor += 1
            If rc.indexLast <> 0 Then
                Dim val = dst3.Get(Of Byte)(rc.maxDist.Y, rc.maxDist.X)
                If val = 0 Then
                    dst3(rc.rect).SetTo(255, rc.mask)
                    unMatchedCells += 1
                    frameLoc.Add(rc.maxDist)
                    framecounts.Add(myFrameCount)
                End If
            End If
        Next
        If standaloneTest() Then
            For i = 0 To framecounts.Count - 1
                SetTrueText(CStr(framecounts(i)), frameLoc(i), 2)
            Next
        End If
        changedCellCounts.Add(unMatchedCells)

        If task.heartBeat Then
            dst3.SetTo(0)
            framecounts.Clear()
            frameLoc.Clear()
            myFrameCount = 0
            Dim sum = changedCellCounts.Sum(), avg = If(changedCellCounts.Count > 0, changedCellCounts.Average(), 0)
            labels(3) = CStr(sum) + " new/moved cells in the last second " + Format(avg, fmt1) + " changed per frame"
            labels(2) = CStr(task.redList.rcList.Count) + " cells, unmatched cells = " + CStr(unMatchedCells) + "   " +
                        CStr(mostlyColor) + " cells were mostly color and " + CStr(task.redList.rcList.Count - mostlyColor) + " had depth."
            changedCellCounts.Clear()
        End If
    End Sub
End Class