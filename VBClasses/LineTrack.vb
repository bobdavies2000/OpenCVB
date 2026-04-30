Imports System.Runtime.InteropServices
Imports System.Windows.Forms.Design.AxImporter
Imports cv = OpenCvSharp
Public Class LineTrack_Basics : Inherits TaskParent
    Public lp As lpData
    Public lpNew As lpData
    Public diffX As Integer
    Public diffY As Integer
    Public Sub New()
        desc = "Search for the requested line in the previous frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone

        If task.lines.lpList.Count = 0 Then Exit Sub

        If standalone And task.heartBeatLT Then lp = task.lines.lpList(0)
        Dim scores As New List(Of Single)
        For Each candidate In task.lines.lpList
            Dim centerScore = lp.ptCenter.DistanceTo(candidate.ptCenter)
            Dim sameEndpointOrder = lp.p1.DistanceTo(candidate.p1) + lp.p2.DistanceTo(candidate.p2)
            Dim swappedEndpointOrder = lp.p1.DistanceTo(candidate.p2) + lp.p2.DistanceTo(candidate.p1)
            Dim endpointScore = Math.Min(sameEndpointOrder, swappedEndpointOrder)
            Dim angleScore = Math.Abs(lp.angle - candidate.angle) * 5.0F
            Dim totalScore = centerScore + endpointScore + angleScore
            scores.Add(totalScore)
        Next

        Dim bestindex = scores.IndexOf(scores.Min)
        lpNew = task.lines.lpList(bestindex)

        If Math.Abs(lpNew.length - lp.length) > lp.length / 4 Then
            lpNew = Nothing
            Exit Sub
        End If
        If Math.Abs(lpNew.angle - lp.angle) > 3 Then lpNew = Nothing

        DrawLine(dst2, lp.p1, lp.p2, task.highlight, task.lineWidth + 1)
        labels(2) = "Tracking line length = " + Format(lp.length, fmt1) + " angle = " + Format(lp.angle, fmt1)
    End Sub
End Class






Public Class LineTrack_TrackCorrelation : Inherits TaskParent
    Dim match As New LineTrack_Correlation
    Public correlation As New Single
    Public lp As lpData
    Public Sub New()
        task.fOptions.MatchCorrSlider.Value = 90
        desc = "Track each of the lines found in Line_Basics_TA"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        dst3 = task.rightView
        match.lpInput = lp
        match.Run(src)
        If match.p1Correlation > task.fCorrThreshold And match.p2Correlation > task.fCorrThreshold Then
            DrawLine(dst2, lp.p1, lp.p2)
        End If
        dst2.Rectangle(lp.rect, white, task.lineWidth)
        DrawLine(dst2, lp.p1, lp.p2)

        labels(2) = match.labels(2)
        dst3 = task.lines.dst3
        labels(3) = task.lines.labels(3)
    End Sub
End Class





Public Class NR_LineTrack_Concat : Inherits TaskParent
    Public lpInput As lpData
    Dim match As New Match_Basics
    Public correlation As Single
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        labels(3) = "Correlation measures how similar the previous template is to the current one."
        desc = "Concatenate the end point templates to return a single correlation to the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If

        If standalone Then lpInput = task.lines.lpList(0)

        Dim nabeRect1 = task.gridNabeRects(task.gridMap.Get(Of Integer)(lpInput.p1.Y, lpInput.p1.X))
        Dim nabeRect2 = task.gridNabeRects(task.gridMap.Get(Of Integer)(lpInput.p2.Y, lpInput.p2.X))
        cv.Cv2.HConcat(src(nabeRect1), src(nabeRect2), match.template)
        Static templateLast = match.template.Clone

        match.Run(templateLast)
        correlation = match.correlation

        If standaloneTest() Then
            Static rFit As New Rectangle_Fit
            rFit.Run(match.template)
            dst2 = rFit.dst2.Clone
            labels(2) = "Correlation = " + Format(correlation, fmt3)

            rFit.Run(templateLast)
            dst3 = rFit.dst2

            dst1 = src.Clone
            DrawRect(dst1, nabeRect1)
            DrawRect(dst1, nabeRect2)
        End If

        templateLast = match.template.Clone
    End Sub
End Class





Public Class LineTrack_Correlation : Inherits TaskParent
    Public lpInput As lpData
    Dim match As New Match_Basics
    Public p1Correlation As Single
    Public p2Correlation As Single
    Public Sub New()
        desc = "Compare area around end points of a line to the previous image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then lpInput = task.lines.lpList(0)
        Static lastImage = task.gray.Clone

        Dim p1GridIndex = task.gridMap.Get(Of Integer)(lpInput.p1.Y, lpInput.p1.X)
        Dim rect = task.gridRects(p1GridIndex)
        match.template = task.gray(rect)
        match.Run(lastImage(task.gridNabeRects(p1GridIndex)))
        p1Correlation = match.correlation

        Dim p2GridIndex = task.gridMap.Get(Of Integer)(lpInput.p2.Y, lpInput.p2.X)
        rect = task.gridRects(p2GridIndex)
        match.template = task.gray(rect)
        match.Run(lastImage(task.gridNabeRects(p2GridIndex)))
        p2Correlation = match.correlation

        lastImage = task.gray.Clone

        If standaloneTest() Then
            dst2 = src.Clone
            DrawLine(dst2, lpInput, task.highlight)
        End If
        labels(2) = "Rect for p1 has correlation " + Format(p1Correlation, fmt3) +
                        " to the previous image while " +
                        "rect for p2 has " + Format(p2Correlation, fmt3)
    End Sub
End Class




Public Class LineTrack_Match : Inherits TaskParent
    Public lpListLast As List(Of lpData) = New List(Of lpData)(task.lines.lpList)
    Public lpList As New List(Of lpData)
    Public lpMatches As New List(Of lpData)
    Dim slices As New LineTrack_Slices
    Public lpMapLast = task.lines.dst1.Clone
    Public Sub New()
        desc = "Match lines with image slices to locate the best matching line.  Confirm with angle."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count <= 1 Then Exit Sub

        slices.Run(emptyMat)

        Dim lpMatch As lpData
        lpMatches.Clear()
        lpList.Clear()
        Dim missCount As Integer
        dst2.SetTo(0)
        dst3.SetTo(0)
        For i = 0 To Math.Min(task.lines.lpList.Count, slices.xSlices.Count) - 1
            Dim lp = task.lines.lpList(i)
            Dim color = task.scalarColors(lp.index + 1)

            For Each sliceSet In {slices.xSlices(i), slices.ySlices(i)}
                Dim angleDelta As New List(Of Single)
                Dim lineIndex As New List(Of Integer)
                For j = 0 To sliceSet.Count - 1
                    Dim lastIndex = sliceSet(j) - 1
                    If lastIndex >= 0 And lastIndex < lpListLast.Count Then
                        lpMatch = lpListLast(lastIndex)
                        angleDelta.Add(Math.Abs(lp.angle - lpMatch.angle))
                        lineIndex.Add(lastIndex)
                    End If
                Next

                If angleDelta.Count > 0 Then
                    Dim minAngleDelta = angleDelta.Min
                    If minAngleDelta < 5 Then ' within 5 degrees of the original line's angle
                        Dim index = lineIndex(angleDelta.IndexOf(minAngleDelta))
                        lpMatch = lpListLast(index)
                        dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)
                        dst3.Line(lpMatch.p1, lpMatch.p2, color, task.lineWidth + 2, task.lineType)
                        lpList.Add(lp)
                        lpMatches.Add(lpMatch)
                    Else
                        missCount += 1
                    End If
                Else
                    missCount += 1
                End If
            Next
        Next

        If task.heartBeat Then
            labels(2) = "Searching " + CStr(slices.lineMaxOffset) + " pixels around center "
            labels(3) = CStr(lpMatches.Count) + " lines matched."
        End If

        lpListLast = New List(Of lpData)(task.lines.lpList)
    End Sub
End Class






Public Class NR_LineTrack_Tester : Inherits TaskParent
    Dim match As New LineTrack_Match
    Public Sub New()
        task.gOptions.DebugCheckBox.Checked = True
        desc = "Test the line match algorithm by just occasionally capturing the current state."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.lines.lpList.Count <= 1 Then Exit Sub

        Static lpListLast As New List(Of lpData)(task.lines.lpList)
        Static srcLast = src.Clone
        Static lpListTest As New List(Of lpData)(task.lines.lpList)
        Static lpMapTest = task.lines.dst1.Clone
        If task.optionsChanged Or task.gOptions.DebugCheckBox.Checked Then
            lpListTest = New List(Of lpData)(task.lines.lpList)
            lpMapTest = task.lines.dst1.Clone
            srcLast = src.Clone
        End If

        ' task.gOptions.DebugCheckBox.Checked = task.heartBeatLT

        match.lpListLast = New List(Of lpData)(lpListTest)
        match.lpMapLast = lpMapTest.clone
        match.Run(task.gray)

        If task.toggleOn Then
            dst2 = src
            dst3 = srcLast.clone
        Else
            dst2.SetTo(0)
            dst3.SetTo(0)
        End If
        For i = 0 To match.lpList.Count - 1
            Dim lp = match.lpList(i)
            Dim color = task.scalarColors(lp.index + 1)
            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)
            dst2.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)

            lp = match.lpMatches(i)
            dst3.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)
            dst3.Line(lp.p1, lp.p2, white, task.lineWidth, task.lineType)
        Next
        labels(2) = match.labels(2)
        labels(3) = match.labels(3)

        task.gOptions.DebugCheckBox.Checked = False
    End Sub
End Class





Public Class LineTrack_Slices : Inherits TaskParent
    Public xSlices As New List(Of List(Of Byte))
    Public ySlices As New List(Of List(Of Byte))
    Public lineMaxOffset As Integer = 10 ' how many pixels to search for lines.
    Public Sub New()
        labels(2) = "White lines are slices used to find previous line locations."
        desc = "Build slices in X and Y from the previous image near the each line's center."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lpMapLast As cv.Mat = task.lines.dst1.Clone

        dst2.SetTo(0)
        xSlices.Clear()
        ySlices.Clear()
        For i = 0 To Math.Min(task.lines.lpList.Count, 5) - 1
            Dim lp = task.lines.lpList(i)
            Dim color = task.scalarColors(lp.index + 1)
            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 2, task.lineType)

            Dim ptMinX = New cv.Point(Math.Max(lp.ptCenter.X - lineMaxOffset, 0), lp.ptCenter.Y)
            Dim ptMaxX = New cv.Point(Math.Min(lp.ptCenter.X + lineMaxOffset, dst2.Width), lp.ptCenter.Y)

            Dim rX = New cv.Rect(ptMinX.X, lp.ptCenter.Y, ptMaxX.X - ptMinX.X, 1)
            Dim SliceX(rX.Width - 1) As Byte
            Marshal.Copy(lpMapLast(rX).Data, SliceX, 0, SliceX.Length)
            dst2.Line(ptMinX, ptMaxX, white, task.lineWidth)

            xSlices.Add(SliceX.ToList)

            Dim ptMinY = New cv.Point(lp.ptCenter.X, Math.Max(lp.ptCenter.Y - lineMaxOffset, 0))
            Dim ptMaxY = New cv.Point(lp.ptCenter.X, Math.Min(lp.ptCenter.Y + lineMaxOffset, dst2.Height))

            Dim rY = New cv.Rect(lp.ptCenter.X, ptMinY.Y, 1, ptMaxY.Y - ptMinY.Y)
            Dim SliceY(rY.Height - 1) As Byte
            Marshal.Copy(lpMapLast(rY).Data, SliceY, 0, SliceY.Length)
            dst2.Line(ptMinY, ptMaxY, white, task.lineWidth)

            ySlices.Add(SliceY.ToList)
        Next

        lpMapLast = task.lines.dst1.Clone
    End Sub
End Class





Public Class LineTrack_Rect : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public lpInput1 As lpData
    Public lpInput2 As lpData
    Public rotatedRect As cv.RotatedRect
    Public Sub New()
        desc = "Create a rectangle from 2 lines"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        If standalone Then
            Dim p1 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p2 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p3 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            Dim p4 = New cv.Point2f(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))
            lpInput1 = New lpData(p1, p2)
            lpInput2 = New lpData(p3, p4)
        End If

        Dim inputPoints() As cv.Point2f = {lpInput1.p1, lpInput1.p2, lpInput2.p1, lpInput2.p2}
        rotatedRect = cv.Cv2.MinAreaRect(inputPoints)
        If standalone And task.heartBeat Then
            dst2.SetTo(0)
            For Each pt In inputPoints
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
            Next
            DrawLine(dst2, lpInput1.p1, lpInput1.p2)
            DrawLine(dst2, lpInput2.p1, lpInput2.p2)
            SetTrueText("Line 1", lpInput1.p1, 2)
            SetTrueText("Line 2", lpInput2.p1, 2)
            Draw_Arc.DrawRotatedOutline(rotatedRect, dst2, cv.Scalar.Yellow)
        End If
    End Sub
End Class









Public Class NR_LineTrack_CenterNeighbor : Inherits TaskParent
    Dim bricks As New Brick_Basics
    Public options As New Options_LineRect
    Public Sub New()
        desc = "Remove lines which have similar depth in bricks on either side of a line."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        bricks.Run(src)
        options.Run()

        dst2 = src.Clone
        dst3 = src.Clone

        Dim depthThreshold = options.depthThreshold
        Dim depthLines As Integer, colorLines As Integer
        For Each lp In task.lines.lpList
            Dim center = New cv.Point((lp.p1.X + lp.p2.X) \ 2, (lp.p1.Y + lp.p2.Y) \ 2)
            Dim index As Integer = task.gridMap.Get(Of Integer)(center.Y, center.X)
            Dim nabeList = task.gridNabes(index)
            Dim foundObjectLine As Boolean = False
            For i = 1 To nabeList.Count - 1
                Dim brick1 = bricks.brickList(nabeList(i))
                If brick1.depth = 0 Then Continue For
                For j = i + 1 To nabeList.Count - 1
                    Dim brick2 = bricks.brickList(nabeList(j))
                    If brick2.depth = 0 Then Continue For
                    If Math.Abs(brick1.depth - brick2.depth) > depthThreshold Then
                        foundObjectLine = True
                        Exit For
                    End If
                Next
                If foundObjectLine Then Exit For
            Next
            If foundObjectLine Then
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                depthLines += 1
            Else
                dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                colorLines += 1
            End If
        Next

        If task.heartBeat Then
            labels(2) = CStr(depthLines) + " lines were found between objects (External Lines)"
            labels(3) = CStr(colorLines) + " internal lines were indentified and are not likely important"
        End If
    End Sub
End Class







Public Class NR_LineTrack_CenterRange : Inherits TaskParent
        Public options As New Options_LineRect
        Dim bricks As New Brick_Basics
        Public Sub New()
            desc = "Remove lines which have similar depth in bricks on either side of a line."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            bricks.Run(src)

            dst2 = src.Clone
            dst3 = src.Clone

            Dim depthThreshold = options.depthThreshold
            Dim depthLines As Integer, colorLines As Integer
            For Each lp In task.lines.lpList
                Dim center = New cv.Point((lp.p1.X + lp.p2.X) \ 2, (lp.p1.Y + lp.p2.Y) \ 2)
                Dim index As Integer = task.gridMap.Get(Of Integer)(center.Y, center.X)
                Dim brick = bricks.brickList(index)
                If brick.mm.maxVal - brick.mm.minVal > depthThreshold Then
                    dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                    depthLines += 1
                Else
                    dst3.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, cv.LineTypes.Link4)
                    colorLines += 1
                End If
            Next

            If task.heartBeat Then
                labels(2) = CStr(depthLines) + " external lines found with gaps in depth."
                labels(3) = CStr(colorLines) + " internal lines found with similar depth on both sides"
            End If
        End Sub
    End Class





Public Class LineTrack_Top3 : Inherits TaskParent
    Dim lineT As New LineTrack_Basics
    Dim lpList As New List(Of lpData)
    Dim match As New Match_Basics
    Public Sub New()
        labels(2) = "Longest = yellow, second is white, third is red"
        desc = "Track the top 3 lines across frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone
        If src.Channels <> 1 Then src = task.gray.Clone

        Static lastImage = task.gray.Clone

        If task.heartBeat Then
            lpList.Clear()
            For i = 0 To 2
                lpList.Add(task.lines.lpList(i))
            Next
            dst3.SetTo(0)
            strOut = ""
        End If

        Dim newList As New List(Of lpData)
        For i = 0 To lpList.Count - 1
            lineT.lp = lpList(i)
            lineT.Run(src)
            Select Case i
                Case 0
                    dst2.Line(lineT.lp.p1, lineT.lp.p2, yellow, task.lineWidth, task.lineType)
                    dst3.Line(lineT.lp.p1, lineT.lp.p2, yellow, task.lineWidth, task.lineType)
                Case 1
                    dst2.Line(lineT.lp.p1, lineT.lp.p2, white, task.lineWidth, task.lineType)
                    dst3.Line(lineT.lp.p1, lineT.lp.p2, white, task.lineWidth, task.lineType)
                Case 2
                    dst2.Line(lineT.lp.p1, lineT.lp.p2, red, task.lineWidth, task.lineType)
                    dst3.Line(lineT.lp.p1, lineT.lp.p2, red, task.lineWidth, task.lineType)
            End Select

            match.template = src(lineT.lp.rect)
            match.Run(lastImage(lineT.lp.rect))
            If match.correlation > task.fCorrThreshold Then
                newList.Add(lineT.lp)
            Else
                strOut += "Lost line " + CStr(i) + " because correlation was " +
                           Format(match.correlation, fmt2) + vbCrLf
            End If
        Next

        SetTrueText(strOut, 3)
        lpList = New List(Of lpData)(newList)
        lastImage = task.gray.Clone
    End Sub
End Class






Public Class LineTrack_SearchX : Inherits TaskParent
    Dim lastImage As cv.Mat
    Dim searchCount As Integer = 9
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Confirm any camera motion using the task.lines output.  Needs work!"
    End Sub
    Private Function testOffset(compareIndex As Integer, i As Integer) As Integer
        Dim r1 = New cv.Rect(compareIndex, 0, dst1.Width - searchCount, dst1.Height)
        Dim r2 = New cv.Rect(i, 0, dst1.Width - searchCount, dst1.Height)

        dst1 = task.lines.dst3.Clone
        dst1(r1).SetTo(0, lastImage(r2))

        Return dst1.CountNonZero
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        SetTrueText(strOut, 2)
        If task.heartBeatLT = False Then Exit Sub
        If task.firstPass Then lastImage = task.lines.dst3.Clone

        dst3 = task.lines.dst3

        Dim countList As New List(Of (count As Integer, index As Integer))
        strOut = ""
        For i = 0 To searchCount
            Dim count = testOffset(0, i)
            strOut += CStr(count) + " for offset " + CStr(i) + vbCrLf
            countList.Add((count, -i))
        Next

        For i = 1 To searchCount
            Dim count = testOffset(searchCount, searchCount - i)
            strOut += CStr(count) + " for offset " + CStr(-i) + vbCrLf
            countList.Add((count, i))
        Next

        Dim bestCount = countList.MinBy(Function(x) x.count)
        strOut += CStr(bestCount.index) + " is the index with the best count" + vbCrLf

        Dim rect As cv.Rect
        If bestCount.index < 0 Then
            rect = New cv.Rect(searchCount + bestCount.index, 0, dst2.Width - searchCount + bestCount.index,
                               dst2.Height)
        ElseIf bestCount.index > 0 Then
            rect = New cv.Rect(searchCount - bestCount.index, 0, dst2.Width - searchCount - bestCount.index,
                               dst2.Height)
        Else
            rect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
        End If

        If task.firstPass Or bestCount.index = 0 Then
            dst2 = lastImage(rect)
        Else
            cv.Cv2.AddWeighted(lastImage(rect), 0.75, dst1(New cv.Rect(0, 0, rect.Width, dst2.Height)),
                                                0.25, 0, dst2)
        End If

        lastImage = task.lines.dst3.Clone
    End Sub
End Class





Public Class LineTrack_Changes : Inherits TaskParent
    Public Sub New()
        labels(2) = "Move camera or wave at camera to see the impact on the lines."
        labels(3) = "Current lines.  dst2 is the difference between lines in current vs. previous image."
        desc = "Track the changes in the lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static lastImage = task.lines.dst3
        dst2 = lastImage.setto(0, task.lines.dst3)

        dst3 = task.lines.dst2

        lastImage = task.lines.dst3.Clone
    End Sub
End Class
