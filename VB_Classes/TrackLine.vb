Imports cv = OpenCvSharp
Public Class TrackLine_Basics : Inherits TaskParent
    Dim match As New Match_Basics
    Dim matchRect As cv.Rect
    Public rawLines As New LineRGB_Raw
    Dim lplist As List(Of lpData)
    Dim knn As New KNN_NNBasics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        OptionParent.FindSlider("KNN Dimension").Value = 6
        desc = "Track an individual line as best as possible."
    End Sub
    Private Function restartLine(src As cv.Mat) As lpData
        For Each lpTemp In lplist
            If lpTemp.gravityProxy Then
                matchRect = lpTemp.rect
                match.template = src(matchRect)
                Return lpTemp
            End If
        Next
        Return New lpData
    End Function
    Private Sub prepEntry(knnList As List(Of Single), lpNext As lpData)
        Dim brick1 = task.grid.gridMap.Get(Of Single)(lpNext.p1.Y, lpNext.p1.X)
        Dim brick2 = task.grid.gridMap.Get(Of Single)(lpNext.p2.Y, lpNext.p2.X)
        knnList.Add(lpNext.p1.X)
        knnList.Add(lpNext.p1.Y)
        knnList.Add(lpNext.p2.X)
        knnList.Add(lpNext.p2.Y)
        knnList.Add(brick1)
        knnList.Add(brick2)
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lplist = task.lineRGB.lpList
        If lplist.Count = 0 Then Exit Sub

        Static lp As New lpData, lpLast As lpData
        lpLast = lp

        If match.correlation < task.fCorrThreshold Or matchRect.Width <= 1 Then ' Or task.heartBeatLT 
            lp = restartLine(src)
        End If

        match.Run(src)

        knn.trainInput.Clear()
        For Each nextlp In task.lineRGB.lpList
            prepEntry(knn.trainInput, nextlp)
        Next

        knn.queries.Clear()
        prepEntry(knn.queries, lp)
        knn.Run(emptyMat)

        lp = task.lineRGB.lpList(knn.result(0, 0))

        If standaloneTest() Then
            dst2 = src.Clone
            DrawCircle(dst2, match.newCenter, task.DotSize, white)
            dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
            dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
            dst3 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            SetTrueText(Format(match.correlation, fmt3), match.newCenter)

            If Math.Abs(lp.age - lpLast.age) <= 1 Then
                dst2.Rectangle(lp.rect, task.highlight, task.lineWidth)
            Else
                dst2.Rectangle(matchRect, red, task.lineWidth)
            End If
        End If

        dst1 = ShowPaletteNoZero(task.lineRGB.lpMap)
        dst1.Circle(lp.center, task.DotSize, task.highlight, task.lineWidth, task.lineType)

        labels(2) = "Selected line has a correlation of " + Format(match.correlation, fmt3) + " with the previous frame."
    End Sub
End Class






Public Class TrackLine_BasicsSimple : Inherits TaskParent
    Dim lp As New lpData
    Dim match As New Match_Basics
    Public rawLines As New LineRGB_Raw
    Dim matchRect As cv.Rect
    Public Sub New()
        desc = "Track an individual line as best as possible."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lineRGB.lpList
        If lplist.Count = 0 Then Exit Sub

        If standalone Then
            If lplist(0).length > lp.length Then
                lp = lplist(0)
                matchRect = lp.rect
                match.template = src(matchRect)
            End If
        End If

        If matchRect.Width <= 1 Then Exit Sub ' nothing yet...
        match.Run(src)
        matchRect = match.newRect

        If standaloneTest() Then
            dst2 = src
            DrawCircle(dst2, match.newCenter, task.DotSize, white)
            dst2.Rectangle(matchRect, task.highlight, task.lineWidth)
            dst3 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            SetTrueText(Format(match.correlation, fmt3), match.newCenter)
        End If

        rawLines.Run(src(matchRect))
        If rawLines.lpList.Count > 0 Then lp = rawLines.lpList(0)
        dst2(matchRect).Line(lp.p1, lp.p2, task.highlight, task.lineWidth, task.lineType)
    End Sub
End Class





Public Class TrackLine_BasicsOld : Inherits TaskParent
    Public lpInput As lpData
    Public foundLine As Boolean
    Dim match As New Match_Line
    Public rawLines As New LineRGB_Raw
    Public Sub New()
        desc = "Track an individual line as best as possible."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lplist = task.lineRGB.lpList
        If lplist.Count = 0 Then Exit Sub
        If standalone And foundLine = False Then lpInput = task.gravityBasics.gravityRGB

        Static subsetrect = lpInput.rect
        If subsetrect.width <= dst2.Height / 10 Then
            lpInput = task.gravityBasics.gravityRGB
            subsetrect = New cv.Rect(0, 0, dst2.Width, dst2.Height)
            Exit Sub
        End If

        Dim lpLast = lpInput

        Dim index = task.lineRGB.lpMap.Get(Of Byte)(lpInput.center.Y, lpInput.center.X)
        If index > 0 Then
            Dim lp = lplist(index - 1)
            If lpInput.ID = lp.ID Then
                foundLine = True
            Else
                match.lpInput = lpInput
                match.Run(src)

                foundLine = match.correlation1 >= task.fCorrThreshold And match.correlation2 >= task.fCorrThreshold
                If foundLine Then
                    lpInput = match.lpOutput
                    subsetrect = lpInput.rect
                End If
            End If
        Else
            rawLines.Run(src(subsetrect))
            dst3(subsetrect) = rawLines.dst2(subsetrect)
            If rawLines.lpList.Count > 0 Then
                Dim p1 = New cv.Point(CInt(rawLines.lpList(0).p1.X + subsetrect.X), CInt(rawLines.lpList(0).p1.Y + subsetrect.Y))
                Dim p2 = New cv.Point(CInt(rawLines.lpList(0).p2.X + subsetrect.X), CInt(rawLines.lpList(0).p2.Y + subsetrect.Y))
                lpInput = New lpData(p1, p2)
            Else
                lpInput = lplist(0)
            End If

            Dim deltaX1 = Math.Abs(task.gravityIMU.ep1.X - lpInput.ep1.X)
            Dim deltaX2 = Math.Abs(task.gravityIMU.ep2.X - lpInput.ep2.X)
            If Math.Abs(deltaX1 - deltaX2) > task.gravityBasics.options.pixelThreshold Then
                lpInput = task.gravityBasics.gravityRGB
            End If
            subsetrect = lpInput.rect
        End If

        dst2 = src
        dst2.Line(lpInput.p1, lpInput.p2, task.highlight, task.lineWidth + 1, task.lineType)
        dst2.Rectangle(subsetrect, task.highlight, task.lineWidth)
    End Sub
End Class


