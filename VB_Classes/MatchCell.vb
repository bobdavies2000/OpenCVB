Imports cv = OpenCvSharp
Public Class MatchCell_Basics : Inherits VB_Algorithm
    Public lrFeat As New FeatureLeftRight_Basics
    Public flood As New Flood_LeftRight
    Public Sub New()
        labels(3) = "Click any cell with a highlighted feature point to see the cell that matches it in the right view."
        desc = "Match RedCloud cells in left and right images using features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        labels(2) = flood.labels(2)

        lrFeat.Run(src)
        For Each mp In lrFeat.mpList
            dst2.Circle(mp.p1, task.dotSize, task.highlightColor, -1, task.lineType)
        Next

        If task.mousePicTag = 2 Then setSelectedContour(flood.cellsLeft, flood.mapLeft)
    End Sub
End Class





Public Class MatchCell_Basics1 : Inherits VB_Algorithm
    Public feat As New MatchCell_Features
    Public redC As New Flood_LeftRight
    Public Sub New()
        desc = "Match RedCloud cells in left and right images using features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst3

        feat.Run(src)

        For i = 0 To redC.cellsLeft.Count - 1
            redC.cellsLeft(i).features.Clear()
        Next

        For Each pt In feat.featLeft
            Dim index = redC.mapLeft.Get(Of Byte)(pt.Y, pt.X)
            redC.cellsLeft(index).features.Add(New cv.Point(pt.X, pt.Y))
        Next

        For i = 0 To redC.cellsRight.Count - 1
            redC.cellsRight(i).features.Clear()
        Next

        For Each pt In feat.featRight
            Dim index = redC.mapRight.Get(Of Byte)(pt.Y, pt.X)
            redC.cellsRight(index).features.Add(New cv.Point(pt.X, pt.Y))
        Next

        For Each rc In redC.cellsLeft
            For Each pt In rc.features
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            Next
        Next

        For Each rc In redC.cellsRight
            For Each pt In rc.features
                dst3.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            Next
        Next
    End Sub
End Class





Public Class MatchCell_Features : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public featLeft As New List(Of cv.Point2f)
    Public featRight As New List(Of cv.Point2f)
    Public Sub New()
        labels(3) = "Right image with matched points"
        desc = "Find GoodFeatures in the left and right images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(task.leftView)
        dst2 = task.leftView
        Dim tmpLeft = New List(Of cv.Point2f)(task.features)

        feat.Run(task.rightView)
        dst3 = task.rightView
        Dim tmpRight = New List(Of cv.Point2f)(task.features)
        Dim leftY As New List(Of Integer)
        For Each pt In tmpLeft
            leftY.Add(pt.Y)
        Next

        Dim rightY As New List(Of Integer)
        For Each pt In tmpRight
            rightY.Add(pt.Y)
        Next

        featLeft.Clear()
        featRight.Clear()

        For i = 0 To leftY.Count - 1
            If rightY.Contains(leftY(i)) Then
                featLeft.Add(tmpLeft(i))
                Dim index = rightY.IndexOf(leftY(i))
                featRight.Add(tmpRight(index))
            End If
        Next

        For Each pt In featLeft
            dst2.Circle(pt, task.dotSize + 1, task.highlightColor, -1, task.lineType)
        Next

        For Each pt In featRight
            dst3.Circle(pt, task.dotSize + 1, task.highlightColor, -1, task.lineType)
        Next

        labels(2) = "Found " + CStr(featLeft.Count) + " features in the left Image with matching points in the right image"
    End Sub
End Class







Public Class MatchCell_CellFeatures : Inherits VB_Algorithm
    Public feat As New Feature_Good
    Public featLeft As New List(Of cv.Point2f)
    Public featRight As New List(Of cv.Point2f)
    Public flood As New Flood_LeftRight
    Public Sub New()
        redOptions.IdentifyCells.Checked = False
        desc = "Find GoodFeatures in the RedCloud cells of the left and right images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        dst3 = flood.dst3

        Dim rightX As New List(Of Integer)
        Dim rightY As New List(Of Integer)
        Dim rightIndex As New List(Of Integer)
        For i = 0 To flood.cellsRight.Count - 1
            Dim rc = flood.cellsRight(i)
            Dim tmp = New cv.Mat(rc.rect.Size, task.rightView.Type, 0)
            task.rightView(rc.rect).CopyTo(tmp, rc.mask)
            feat.Run(tmp)
            For Each pt In task.features
                If rightY.Contains(pt.Y) = False Then
                    rightX.Add(rc.rect.X + pt.X)
                    rightY.Add(rc.rect.Y + pt.Y)
                    rightIndex.Add(i)
                End If
            Next
        Next

        For i = 0 To flood.cellsLeft.Count - 1
            Dim rc = flood.cellsLeft(i)
            Dim tmp As New cv.Mat(rc.rect.Size, task.leftView.Type, 0)
            task.leftView(rc.rect).CopyTo(tmp, rc.mask)
            feat.Run(tmp)
            Dim rcChanged As Boolean = False
            For Each p1 In task.features
                Dim p2 = New cv.Point(p1.X + rc.rect.X, p1.Y + rc.rect.Y)
                If rightY.Contains(p2.Y) = False Then Continue For
                For j = 0 To rightY.Count - 1
                    If rightY(j) = p2.Y Then
                        rc.featurePair.Add(New pointPair(p2, New cv.Point(rightX(j), rightY(j))))
                        rc.matchCandidates.Add(rightIndex(j))
                        rcChanged = True
                    End If
                Next
            Next
            If rcChanged Then flood.cellsLeft(i) = rc
        Next

        If task.mouseClickFlag Then redOptions.IdentifyCells.Checked = True
        If redOptions.IdentifyCells.Checked Then
            setSelectedContour(flood.cellsLeft, flood.mapLeft)
            dst2(task.rc.rect).SetTo(task.highlightColor, task.rc.mask)

            For Each index In task.rc.matchCandidates
                Dim rc = flood.cellsRight(index)
                dst3(rc.rect).SetTo(task.highlightColor, rc.mask)
            Next

            For Each mp In task.rc.featurePair
                dst2.Circle(mp.p1, task.dotSize + 3, cv.Scalar.Black, -1, task.lineType)
                dst3.Circle(mp.p2, task.dotSize + 3, cv.Scalar.Black, -1, task.lineType)
            Next
            labels(2) = "Found " + CStr(task.rc.matchCandidates.Count) + " candidates for matching cells."
        Else
            labels(2) = "Click on any cell to see candidate cells with possible matching features"
        End If
    End Sub
End Class





Public Class MatchCell_NearestFeature : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim feat As New FeatureLeftRight_Basics
    Dim knn As New KNN_Core
    Public Sub New()
        desc = "Find hte nearest feature to every cell in task.redCells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst2.clone
        labels(2) = redC.labels(2)

        knn.queries.Clear()
        For Each rc In task.redCells
            knn.queries.Add(rc.maxDStable)
        Next

        knn.trainInput.Clear()
        For Each mp In feat.mpList
            knn.trainInput.Add(New cv.Point2f(mp.p1.X, mp.p1.Y))
        Next

        knn.Run(Nothing)

        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            rc.nearestFeature = knn.trainInput(knn.result(i, 0))
            dst3.Line(rc.nearestFeature, rc.maxDStable, task.highlightColor, task.lineWidth, task.lineType)
        Next
    End Sub
End Class
