Imports cv = OpenCvSharp
Public Class MatchCells_Basics : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public flood As New Flood_LeftRight
    Public Sub New()
        redOptions.IdentifyCells.Checked = False
        desc = "Match RedCloud cells in left and right images using features"
    End Sub
    Public Sub RunVB(src As cv.Mat)

        Dim leftY As New List(Of Integer), rightY As New List(Of Integer)
        Dim leftIndex As New List(Of Integer), rightIndex As New List(Of Integer)
        Dim leftPT As New List(Of cv.Point2f), rightPT As New List(Of cv.Point2f)
        For i = 0 To flood.cellsLeft.Count - 1
            Dim rc = flood.cellsLeft(i)
            feat.Run(task.leftView(rc.rect))
            For Each pt In task.features
                Dim p1 = New cv.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y)
                If leftPT.Contains(p1) = False Then
                    leftPT.Add(p1)
                    leftY.Add(p1.Y)
                    leftIndex.Add(i)
                End If
            Next
        Next

        For i = 0 To flood.cellsRight.Count - 1
            Dim rc = flood.cellsRight(i)
            feat.Run(task.rightView(rc.rect))
            For Each pt In task.features
                Dim p1 = New cv.Point(rc.rect.X + pt.X, rc.rect.Y + pt.Y)
                If rightPT.Contains(p1) = False Then
                    rightPT.Add(p1)
                    rightY.Add(p1.Y)
                    rightIndex.Add(i)
                End If
            Next
            flood.cellsRight(i) = rc
        Next

        'Dim hitCount As Integer
        'For i = 0 To leftY.Count - 1
        '    If rightY.Contains(leftY(i)) Then
        '        Dim rcL = flood.cellsLeft(leftIndex(i))
        '        Dim pt = leftPT(leftIndex(i))
        '        hitCount += 1

        '        Dim index = rightY.IndexOf(leftY(i))
        '        rcL.featurePair.Add(New pointPair(pt, rightPT(index)))
        '        rcL.matchCandidates.Add(rightIndex(index))
        '        flood.cellsLeft(leftIndex(i)) = rcL
        '    End If
        'Next

        'If task.mouseClickFlag Then redOptions.IdentifyCells.Checked = True
        'If redOptions.IdentifyCells.Checked Then
        '    setSelectedContour(flood.cellsLeft, flood.mapLeft)
        '    dst2(task.rc.rect).SetTo(task.highlightColor, task.rc.mask)

        '    For Each index In task.rc.matchCandidates
        '        If index > 1 Then
        '            Dim rc = flood.cellsRight(index)
        '            dst3(rc.rect).SetTo(task.highlightColor, rc.mask)
        '        End If
        '    Next

        '    For Each mp In task.rc.featurePair
        '        dst2.Circle(mp.p1, task.dotSize + 3, cv.Scalar.Black, -1, task.lineType)
        '        dst3.Circle(mp.p2, task.dotSize + 3, cv.Scalar.Black, -1, task.lineType)
        '    Next

        'labels(3) = "There are " + CStr(task.rc.matchCandidates.Count) + " potential matches in the right view."
        'End If

        'labels(2) = CStr(hitCount) + " features were found in both left and right cells."
    End Sub
End Class





Public Class MatchCells_Basics1 : Inherits VB_Algorithm
    Public feat As New MatchCells_Features
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





Public Class MatchCells_Features : Inherits VB_Algorithm
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







Public Class MatchCells_CellFeatures : Inherits VB_Algorithm
    Public feat As New Feature_Basics
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

        Dim leftX As New List(Of Integer), rightX As New List(Of Integer)
        Dim leftY As New List(Of Integer), rightY As New List(Of Integer)
        Dim leftIndex As New List(Of Integer), rightIndex As New List(Of Integer)
        For i = 0 To flood.cellsLeft.Count - 1
            Dim rc = flood.cellsLeft(i)
            feat.Run(task.leftView(rc.rect))
            For Each pt In task.features
                If leftY.Contains(pt.Y) = False Then
                    leftX.Add(rc.rect.X + pt.X)
                    leftY.Add(rc.rect.Y + pt.Y)
                    leftIndex.Add(i)
                End If
            Next
        Next

        For i = 0 To flood.cellsRight.Count - 1
            Dim rc = flood.cellsRight(i)
            feat.Run(task.rightView(rc.rect))
            For Each pt In task.features
                If rightY.Contains(pt.Y) = False Then
                    rightX.Add(rc.rect.X + pt.X)
                    rightY.Add(rc.rect.Y + pt.Y)
                    rightIndex.Add(i)
                End If
            Next
        Next

        For i = 0 To leftY.Count - 1
            If leftIndex(i) = 1 Then Continue For
            If rightY.Contains(leftY(i)) Then
                Dim rc = flood.cellsLeft(leftIndex(i))
                If rc.matchCandidates.Count > 0 Then Continue For
                Dim candidates As New SortedList(Of Integer, Integer)
                For j = 0 To rightY.Count - 1
                    If rightY(j) = leftY(i) Then
                        Dim p1 = New cv.Point(leftX(i), leftY(i))
                        Dim p2 = New cv.Point(rightX(j), rightY(j))
                        If rc.featurePair.ContainsKey(rightX(j)) = False Then
                            rc.featurePair.Add(rightX(j), New pointPair(p1, p2))
                            candidates.Add(rightX(j), rightIndex(j))
                        End If
                    End If
                Next
                For j = 0 To candidates.Count - 1
                    rc.matchCandidates.Add(candidates.ElementAt(j).Key, candidates.ElementAt(j).Value)
                Next
                flood.cellsLeft(leftIndex(i)) = rc
            End If
        Next

        If task.mouseClickFlag Then redOptions.IdentifyCells.Checked = True
        If redOptions.IdentifyCells.Checked Then
            setSelectedContour(flood.cellsLeft, flood.mapLeft)
            dst2(task.rc.rect).SetTo(task.highlightColor, task.rc.mask)

            For Each ele In task.rc.matchCandidates
                Dim rc = flood.cellsRight(ele.Value)
                dst3(rc.rect).SetTo(task.highlightColor, rc.mask)
            Next

            For Each ele In task.rc.featurePair
                Dim mp = ele.value
                dst2.Circle(mp.p1, task.dotSize + 3, cv.Scalar.Black, -1, task.lineType)
                dst3.Circle(mp.p2, task.dotSize + 3, cv.Scalar.Black, -1, task.lineType)
            Next
            labels(2) = "Found " + CStr(task.rc.matchCandidates.Count) + " candidates for matching cells."
        Else
            labels(2) = "Click on any cell to see candidate cells with possible matching features"
        End If
    End Sub
End Class