Imports cv = OpenCvSharp
Public Class MatchCells_Basics : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public flood As New Flood_LeftRight
    Public Sub New()
        desc = "Match RedCloud cells in left and right images using features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        flood.Run(src)
        dst2 = flood.dst2
        dst3 = flood.dst3

        Dim leftY As New List(Of Integer), rightY As New List(Of Integer)
        Dim leftIndex As New List(Of Integer), rightIndex As New List(Of Integer)
        Dim leftPT As New List(Of cv.Point2f), rightPT As New List(Of cv.Point2f)
        For i = 0 To flood.cellsLeft.Count - 1
            Dim rc = flood.cellsLeft(i)
            feat.Run(src(rc.rect))
            For Each pt In task.features
                If leftPT.Contains(pt) = False Then
                    leftPT.Add(pt)
                    leftY.Add(pt.Y)
                    leftIndex.Add(i)
                End If
            Next
        Next

        For i = 0 To flood.cellsRight.Count - 1
            Dim rc = flood.cellsRight(i)
            feat.Run(src(rc.rect))
            For Each pt In task.features
                If rightPT.Contains(pt) = False Then
                    rightPT.Add(pt)
                    rightY.Add(pt.Y)
                    rightIndex.Add(i)
                End If
            Next
            flood.cellsRight(i) = rc
        Next

        Dim hitCount As Integer
        For i = 0 To leftY.Count - 1
            If rightY.Contains(leftY(i)) Then
                Dim rcL = flood.cellsLeft(leftIndex(i))
                Dim pt = leftPT(leftIndex(i))
                If rcL.features.Contains(pt) = False Then
                    hitCount += 1
                    rcL.features.Add(pt)
                End If

                Dim index = rightY.IndexOf(leftY(i))
                Dim rcR = flood.cellsRight(rightIndex(index))
                pt = rightPT(rightIndex(index))
                If rcR.features.Contains(pt) = False Then
                    hitCount += 1
                    rcR.features.Add(pt)
                    flood.cellsRight(rightIndex(index)) = rcR
                    rcL.matchCandidates.Add(rcR.index)
                End If
                flood.cellsLeft(leftIndex(i)) = rcL
            End If
        Next

        If task.mousePicTag = 2 Then
            setSelectedContour(flood.cellsLeft, flood.mapLeft)
            dst2(task.rc.rect).SetTo(task.highlightColor, task.rc.mask)

            For Each index In task.rc.matchCandidates
                Dim rc = flood.cellsRight(index)
                dst3(rc.rect).SetTo(task.highlightColor, rc.mask)
            Next
            labels(3) = "There are " + CStr(task.rc.matchCandidates.Count) + " potential matches in the right view."
        End If

        labels(2) = CStr(hitCount) + " features were found in both left and right cells."
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