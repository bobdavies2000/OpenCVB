﻿Imports System.Windows.Markup
Imports NAudio
Imports cvb = OpenCvSharp
' https://docs.opencvb.org/3.4/d3/dc0/group__imgproc__shape.html
' https://docs.opencvb.org/3.4/d5/d45/tutorial_py_contours_more_functions.html
' https://stackoverflow.com/questions/55529371/opencv-shape-matching-between-two-similar-shapes
Public Class MatchShapes_Basics : Inherits TaskParent
    Public hull1 As cvb.Point()()
    Public hull2 As cvb.Point()()
    Dim match As New Options_MatchShapes
    Dim options As New Options_Contours
    Public Sub New()
        FindRadio("CComp").Checked = True
        FindRadio("FloodFill").Enabled = False
        FindRadio("ApproxNone").Checked = True

        dst0 = cvb.Cv2.ImRead(task.HomeDir + "Data/star1.png", cvb.ImreadModes.Color).CvtColor(cvb.ColorConversionCodes.BGR2Gray)
        dst1 = cvb.Cv2.ImRead(task.HomeDir + "Data/star2.png", cvb.ImreadModes.Color).CvtColor(cvb.ColorConversionCodes.BGR2Gray)
        desc = "MatchShapes compares single hull to single hull - pretty tricky"
    End Sub
    Public Function findBiggestHull(hull As cvb.Point()(), maxLen As Integer, maxIndex As Integer, dst As cvb.Mat) As Integer
        For i = 0 To hull.Length - 1
            If hull(i).Length > maxLen Then
                maxLen = hull(i).Length
                maxIndex = i
            End If
        Next

        For Each p In hull(maxIndex)
            DrawCircle(dst, p, task.DotSize, cvb.Scalar.Yellow)
        Next
        Return maxIndex
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        match.RunOpt()

        If standaloneTest() Then
            dst2 = dst0.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
            dst3 = dst1.CvtColor(cvb.ColorConversionCodes.GRAY2BGR)
        End If

        dst0 = dst0.Threshold(50, 255, cvb.ThresholdTypes.Binary)
        hull1 = cvb.Cv2.FindContoursAsArray(dst0, options.retrievalMode, options.ApproximationMode)

        dst1 = dst1.Threshold(127, 255, cvb.ThresholdTypes.Binary)
        hull2 = cvb.Cv2.FindContoursAsArray(dst1, options.retrievalMode, options.ApproximationMode)

        Dim maxLen1 As Integer, maxIndex1 As Integer, maxLen2 As Integer, maxIndex2 As Integer
        maxIndex1 = findBiggestHull(hull1, maxLen1, maxIndex1, dst2)
        maxIndex2 = findBiggestHull(hull2, maxLen2, maxIndex2, dst3)

        Dim matchVal = cvb.Cv2.MatchShapes(hull1(maxIndex1), hull2(maxIndex2), match.matchOption)
        labels(2) = "MatchShapes returned " + Format(matchVal, fmt2)
    End Sub
End Class











Public Class MatchShapes_NearbyHull : Inherits TaskParent
    Public similarCells As New List(Of rcData)
    Public bestCell As Integer
    Dim rc As New rcData
    Dim options As New Options_MatchShapes
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        labels = {"", "", "Output of RedCloud_Hulls", "Cells similar to selected cell"}
        desc = "MatchShapes: Find all the reasonable matches (< 1.0 for matchVal)"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If standaloneTest() Then
            hulls.Run(task.color)
            If task.redCells.Count = 0 Then Exit Sub
            dst2 = hulls.dst2
            rc = task.rc
        End If

        dst3.SetTo(0)
        similarCells.Clear()

        Dim minMatch As Single = Single.MaxValue
        For Each rc2 In task.redCells
            If rc2.hull Is Nothing Or rc.hull Is Nothing Then Continue For
            If Math.Abs(rc2.maxDist.Y - rc.maxDist.Y) > options.maxYdelta Then Continue For
            Dim matchVal = cvb.Cv2.MatchShapes(rc.hull, rc2.hull, options.matchOption)
            If matchVal < options.matchThreshold Then
                If matchVal < minMatch And matchVal > 0 Then
                    minMatch = matchVal
                    bestCell = similarCells.Count
                End If
                DrawContour(dst3(rc2.rect), rc2.hull, cvb.Scalar.White, -1)
                similarCells.Add(rc2)
            End If
        Next

        If similarCells.Count = 0 Then SetTrueText("No matches with match value < " + Format(options.matchThreshold, fmt2), New cvb.Point(5, 5), 3)
    End Sub
End Class










Public Class MatchShapes_Nearby : Inherits TaskParent
    Public redCells As New List(Of rcData)
    Public similarCells As New List(Of rcData)
    Public bestCell As Integer
    Public rc As New rcData
    Dim options As New Options_MatchShapes
    Public runStandalone As Boolean = False
    Dim redC As New RedCloud_Basics
    Dim addTour As New RedCloud_ContourUpdate
    Public Sub New()
        labels = {"Left floodfill image", "Right floodfill image", "Left image of identified cells", "Right image with identified cells"}
        desc = "MatchShapes: Find matches at similar latitude (controlled with slider)"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        Dim myStandalone = standaloneTest() Or runStandalone

        If myStandalone Then
            redC.Run(task.color)
            If task.redCells.Count = 0 Then Exit Sub
            dst2 = redC.dst2
            addTour.redCells = New List(Of rcData)(task.redCells)
            addTour.Run(src)
            rc = task.rc
        End If

        If task.heartBeat And myStandalone Then dst3.SetTo(0)
        similarCells.Clear()

        If task.gOptions.displayDst0.Checked Then
            dst0 = task.color.Clone
            DrawContour(dst0(rc.rect), rc.contour, task.HighlightColor)
        End If

        Dim minMatch As Single = Single.MaxValue
        bestCell = -1
        For i = 0 To addTour.redCells.Count - 1
            Dim rc2 = addTour.redCells(i)
            If rc2.contour Is Nothing Then Continue For
            Dim matchVal = cvb.Cv2.MatchShapes(rc.contour, rc2.contour, options.matchOption)
            If matchVal < options.matchThreshold Then
                If matchVal < minMatch And matchVal > 0 Then
                    minMatch = matchVal
                    bestCell = similarCells.Count
                End If
                DrawContour(dst3(rc2.rect), rc2.contour, rc2.color, -1)
                similarCells.Add(rc2)
            End If
        Next

        If bestCell >= 0 Then
            Dim rc = similarCells(bestCell)
            DrawCircle(dst3,rc.maxDist, task.DotSize, cvb.Scalar.White)
            SetTrueText("Best match", rc.maxDist, 3)
        End If
        If similarCells.Count = 0 Then SetTrueText("No matches with match value < " + Format(options.matchThreshold, fmt2), New cvb.Point(5, 5), 3)
    End Sub
End Class







Public Class MatchShapes_Hulls : Inherits TaskParent
    Dim options As New Options_MatchShapes
    Dim hulls As New RedCloud_Hulls
    Public Sub New()
        FindSlider("Match Threshold %").Value = 3
        labels = {"", "", "Output of RedCloud_Hulls", "All RedCloud cells that matched the selected cell with the current settings are below."}
        desc = "Find all RedCloud hull shapes similar to the one selected.  Use sliders and radio buttons to see impact."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        hulls.Run(src)
        dst2 = hulls.dst2
        If task.heartBeat Then dst3.SetTo(0)

        Dim rcX = task.rc

        For Each rc In task.redCells
            If rc.hull Is Nothing Or rcX.hull Is Nothing Then Continue For
            Dim matchVal = cvb.Cv2.MatchShapes(rcX.hull, rc.hull, options.matchOption)
            If matchVal < options.matchThreshold Then DrawContour(dst3(rc.rect), rc.hull, cvb.Scalar.White, -1)
        Next
    End Sub
End Class











Public Class MatchShapes_Contours : Inherits TaskParent
    Dim options As New Options_MatchShapes
    Dim redC As New RedCloud_Basics
    Public Sub New()
        FindSlider("Match Threshold %").Value = 3
        labels = {"", "", "Output of RedCloud_Basics", "All RedCloud cells that matched the selected cell with the current settings are below."}
        desc = "Find all RedCloud contours similar to the one selected.  Use sliders and radio buttons to see impact."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        redC.Run(src)
        dst2 = redC.dst2
        If task.heartBeat Then dst3.SetTo(0)

        Dim rcX = task.rc

        For Each rc In task.redCells
            If rc.contour Is Nothing Then Continue For
            Dim matchVal = cvb.Cv2.MatchShapes(rcX.contour, rc.contour, options.matchOption)
            If matchVal < options.matchThreshold Then DrawContour(dst3(rc.rect), rc.contour, cvb.Scalar.White, -1)
        Next
    End Sub
End Class