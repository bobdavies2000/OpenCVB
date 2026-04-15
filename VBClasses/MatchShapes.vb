Imports cv = OpenCvSharp
' https://docs.opencvb.org/3.4/d3/dc0/group__imgproc__shape.html
' https://docs.opencvb.org/3.4/d5/d45/tutorial_py_contours_more_functions.html
' https://stackoverflow.com/questions/55529371/opencv-shape-matching-between-two-similar-shapes
Public Class MatchShapes_Basics : Inherits TaskParent
    Public hull1 As cv.Point()()
    Public hull2 As cv.Point()()
    Dim match As New Options_MatchShapes
    Dim options As New Options_Contours
    Public Sub New()
        OptionParent.findRadio("CComp").Checked = True
        OptionParent.findRadio("FloodFill").Enabled = False
        OptionParent.findRadio("ApproxNone").Checked = True

        dst0 = cv.Cv2.ImRead(task.homeDir + "Data/star1.png", cv.ImreadModes.Color).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        dst1 = cv.Cv2.ImRead(task.homeDir + "Data/star2.png", cv.ImreadModes.Color).CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        desc = "MatchShapes compares single hull to single hull - pretty tricky"
    End Sub
    Public Function findBiggestHull(hull As cv.Point()(), maxLen As Integer, maxIndex As Integer, dst As cv.Mat) As Integer
        For i = 0 To hull.Length - 1
            If hull(i).Length > maxLen Then
                maxLen = hull(i).Length
                maxIndex = i
            End If
        Next

        For Each p In hull(maxIndex)
            DrawCircle(dst, p, task.DotSize, cv.Scalar.Yellow)
        Next
        Return maxIndex
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        match.Run()

        If standaloneTest() Then
            dst2 = dst0.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
            dst3 = dst1.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        End If

        dst0 = dst0.Threshold(50, 255, cv.ThresholdTypes.Binary)
        hull1 = cv.Cv2.FindContoursAsArray(dst0, options.retrievalMode, options.ApproximationMode)

        dst1 = dst1.Threshold(127, 255, cv.ThresholdTypes.Binary)
        hull2 = cv.Cv2.FindContoursAsArray(dst1, options.retrievalMode, options.ApproximationMode)

        Dim maxLen1 As Integer, maxIndex1 As Integer, maxLen2 As Integer, maxIndex2 As Integer
        maxIndex1 = findBiggestHull(hull1, maxLen1, maxIndex1, dst2)
        maxIndex2 = findBiggestHull(hull2, maxLen2, maxIndex2, dst3)

        Dim matchVal = cv.Cv2.MatchShapes(hull1(maxIndex1), hull2(maxIndex2), match.matchOption)
        labels(2) = "MatchShapes returned " + Format(matchVal, fmt2)
    End Sub
End Class






Public Class MatchShapes_Nearby : Inherits TaskParent
    Public similarCells As New List(Of rcData)
    Public rc As New rcData
    Dim options As New Options_MatchShapes
    Dim redC As New RedCloud_Basics
    Public Sub New()
        labels(3) = "All the shapes that match since the last heartbeat."
        desc = "Find similar shapes in the redCloud cells."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        Dim selectMsg = "select any cell to match its shape to other cells"

        If standalone Then
            redC.Run(src)
            dst2 = redC.dst2.Clone
            labels(2) = redC.labels(2)
            If redC.rcList.Count = 0 Or task.rcD Is Nothing Then
                SetTrueText(selectMsg, 3)
                Exit Sub
            End If
        End If
        If task.rcD Is Nothing Then
            SetTrueText(selectMsg, 3)
            Exit Sub
        End If

        Static rcListLast As List(Of rcData)
        If task.heartBeat Then
            rcListLast = New List(Of rcData)(redC.rcList)
            dst3.SetTo(0)
        End If

        similarCells.Clear()

        If task.gOptions.displayDst0.Checked Then
            dst0 = task.color.Clone
        End If

        Dim matchVals As New List(Of Single)

        For i = 0 To rcListLast.Count - 1
            Dim rc2 = rcListLast(i)
            If rc2.contour Is Nothing Then Continue For
            If Math.Abs(rc2.pixels - task.rcD.pixels) < 100 Then
                Dim matchval = cv.Cv2.MatchShapes(task.rcD.contour, rc2.contour, options.matchOption)
                If matchval < options.matchThreshold Then
                    dst3(rc2.rect).SetTo(rc2.color, rc2.mask)
                    similarCells.Add(rc2)
                    matchVals.Add(matchval)
                End If
            End If
        Next

        If matchVals.Count = 0 Then
            SetTrueText(selectMsg, 3)
        Else
            Dim index = matchVals.IndexOf(matchVals.Min)
            Dim rc = similarCells(index)
            DrawCircle(dst3, rc.maxDist, task.DotSize, white)
            If similarCells.Count = 0 Then SetTrueText("No matches with match value < " + Format(options.matchThreshold, fmt2), New cv.Point(5, 5), 3)
        End If
        SetTrueText("Best match", task.rcD.maxDist, 3)
    End Sub
End Class







Public Class NR_MatchShapes_Hulls : Inherits TaskParent
    Dim options As New Options_MatchShapes
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        OptionParent.FindSlider("Match Threshold %").Value = 3
        labels = {"", "", "Output of RedColor_Hulls", "All RedCloud cells that matched the selected cell with the current settings are below."}
        desc = "Find all RedCloud hull shapes similar to the one selected.  Use sliders and radio buttons to see impact."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        hulls.Run(src)
        dst2 = hulls.dst2
        If task.heartBeat Then dst3.SetTo(0)

        Dim rcX = task.rcD

        For Each rc In hulls.rclist
            If rc.hull Is Nothing Or rcX.hull Is Nothing Then Continue For
            Dim matchVal = cv.Cv2.MatchShapes(rcX.hull, rc.hull, options.matchOption)
            If matchVal < options.matchThreshold Then DrawTour(dst3(rc.rect), rc.hull, white, -1)
        Next
    End Sub
End Class











Public Class NR_MatchShapes_Contours : Inherits TaskParent
    Dim options As New Options_MatchShapes
    Dim redC As New RedColor_Basics
    Public Sub New()
        If standalone Then task.gOptions.displayDst1.Checked = True
        OptionParent.FindSlider("Match Threshold %").Value = 3
        labels = {"", "", "Output of RedMask_List", "All RedCloud cells that matched the selected cell with the current settings are below."}
        desc = "Find all RedCloud contours similar to the one selected.  Use sliders and radio buttons to see impact."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        redC.Run(src)
        dst2 = redC.dst2
        labels(2) = redC.labels(2)

        If task.heartBeat Then dst3.SetTo(0)
        SetTrueText(redC.strOut, 1)
        If task.rcD Is Nothing Then
            SetTrueText("Select any cell", 1)
            Exit Sub
        End If

        Dim rcX = task.rcD

        For Each rc In redC.rcList
            If rc.contour Is Nothing Then Continue For
            Dim matchVal = cv.Cv2.MatchShapes(rcX.contour, rc.contour, options.matchOption)
            If matchVal < options.matchThreshold Then DrawTour(dst3(rc.rect), rc.contour, white, -1)
        Next
    End Sub
End Class











Public Class NR_MatchShapes_NearbyHull : Inherits TaskParent
    Public similarCells As New List(Of rcData)
    Public bestCell As Integer
    Dim rc As New rcData
    Dim options As New Options_MatchShapes
    Dim hulls As New RedColor_Hulls
    Public Sub New()
        labels = {"", "", "Output of RedColor_Hulls", "Cells similar to selected cell"}
        desc = "MatchShapes: Find all the reasonable matches (< 1.0 for matchVal)"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then
            hulls.Run(task.color)
            If hulls.rclist.Count = 0 Then Exit Sub
            dst2 = hulls.dst2
            rc = task.rcD
        End If

        dst3.SetTo(0)
        similarCells.Clear()

        Dim minMatch As Single = Single.MaxValue
        For Each rc2 In hulls.rclist
            If rc2.hull Is Nothing Or rc.hull Is Nothing Then Continue For
            If Math.Abs(rc2.maxDist.Y - rc.maxDist.Y) > options.maxYdelta Then Continue For
            Dim matchVal = cv.Cv2.MatchShapes(rc.hull, rc2.hull, options.matchOption)
            If matchVal < options.matchThreshold Then
                If matchVal < minMatch And matchVal > 0 Then
                    minMatch = matchVal
                    bestCell = similarCells.Count
                End If
                DrawTour(dst3(rc2.rect), rc2.hull, white, -1)
                similarCells.Add(rc2)
            End If
        Next

        If similarCells.Count = 0 Then SetTrueText("No matches with match value < " + Format(options.matchThreshold, fmt2), New cv.Point(5, 5), 3)
    End Sub
End Class
