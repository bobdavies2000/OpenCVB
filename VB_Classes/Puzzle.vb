Imports cv = OpenCvSharp
Imports System.Threading
Module Puzzle_Solvers
    Public Enum tileSide
        top
        bottom
        left
        right
        none
    End Enum
    Public Enum cornerType
        upperLeft
        upperRight
        lowerLeft
        lowerRight
        none
    End Enum
    Public Structure bestFit
        Public index As integer
        Public bestMetricUp As Single
        Public bestMetricDn As Single
        Public bestMetricLt As Single
        Public bestMetricRt As Single
        Public bestUp As List(Of Integer)
        Public bestDn As List(Of Integer)
        Public bestLt As List(Of Integer)
        Public bestRt As List(Of Integer)
        Public AvgMetric As Single
        Public MinBest As Single
        Public maxBest As Single
        Public maxBestIndex As Integer
        Public edge As tileSide
        Public corner As cornerType
    End Structure
    Public Class fit
        Public index As integer
        Public neighbor As Integer
        Public metricUp As Single
        Public metricDn As Single
        Public metricLt As Single
        Public metricRt As Single
        Sub New(abs() As Single, _index As integer, n As integer)
            metricUp = abs(0)
            metricDn = abs(1)
            metricLt = abs(2)
            metricRt = abs(3)
            index = _index
            neighbor = n
        End Sub
    End Class
    Public Class CompareSingle : Implements IComparer(Of Single)
        Public Function Compare(ByVal a As Single, ByVal b As Single) As Integer Implements IComparer(Of Single).Compare
            ' why have compare for just unequal?  So we can get duplicates.  Nothing below returns a zero (equal)
            If a <= b Then Return 1
            Return -1
        End Function
    End Class
    Private Function computeMetric(sample() As cv.Mat) As Single()
        Dim tmp As New cv.Mat
        Dim absDiff(4 - 1) As Single
        For i = 0 To 8 - 1 Step 2
            cv.Cv2.Absdiff(sample(i), sample(i + 1), tmp) ' compare the 4 sides so there are 8 sample inputs
            Dim absD = cv.Cv2.Sum(tmp)
            For j = 0 To 3 - 1
                If absDiff(i / 2) < absD.Item(j) Then absDiff(i / 2) = absD.Item(j) ' which channel has the best absdiff
            Next
        Next
        Return absDiff
    End Function
    Public Function getEdgeType(fit As bestFit) As tileSide
        Dim edge As tileSide
        For i = 0 To 4 - 1
            Dim nextBest = Choose(i + 1, fit.bestMetricUp, fit.bestMetricDn, fit.bestMetricLt, fit.bestMetricRt)
            If nextBest = fit.MinBest Then
                edge = Choose(i + 1, tileSide.top, tileSide.bottom, tileSide.left, tileSide.right)
                Exit For
            End If
        Next
        Return edge
    End Function
    Public Function getCornerType(ByRef fit As bestFit) As cornerType
        Dim sortBest As New SortedList(Of Single, Single)(New CompareSingle)
        For i = 0 To 4 - 1
            Dim nextVal = Choose(i + 1, fit.bestMetricUp, fit.bestMetricDn, fit.bestMetricLt, fit.bestMetricRt)
            Dim nextIndex = Choose(i + 1, 1, 2, 3, 4)
            sortBest.Add(nextVal, nextIndex)
        Next

        If sortBest.ElementAt(2).Value = 1 And sortBest.ElementAt(3).Value = 3 Or sortBest.ElementAt(2).Value = 3 And sortBest.ElementAt(3).Value = 1 Then
            Return cornerType.upperLeft
        End If

        If sortBest.ElementAt(2).Value = 1 And sortBest.ElementAt(3).Value = 4 Or sortBest.ElementAt(2).Value = 4 And sortBest.ElementAt(3).Value = 1 Then
            Return cornerType.upperRight
        End If

        If sortBest.ElementAt(2).Value = 2 And sortBest.ElementAt(3).Value = 3 Or sortBest.ElementAt(2).Value = 3 And sortBest.ElementAt(3).Value = 2 Then
            Return cornerType.lowerLeft
        End If

        If sortBest.ElementAt(2).Value = 2 And sortBest.ElementAt(3).Value = 4 Or sortBest.ElementAt(2).Value = 4 And sortBest.ElementAt(3).Value = 2 Then
            Return cornerType.lowerRight
        End If
        Return cornerType.none
    End Function
    Public Function fitCheck(dst1 As cv.Mat, roilist() As cv.Rect, ByRef fitlist As List(Of bestFit)) As List(Of bestFit)
        ' compute absDiff of every top/bottom to every left/right side
        Dim sample(8 - 1) As cv.Mat
        Dim corners As New List(Of bestFit)
        Dim edges As New List(Of Integer)
        Dim sortedCorners As New SortedList(Of Single, bestFit)(New CompareSingle)
        For roiIndex = 0 To roilist.Count - 1
            Dim maxDiff() = {Single.MinValue, Single.MinValue, Single.MinValue, Single.MinValue}
            Dim roi1 = roilist(roiIndex)
            sample(0) = dst1(roi1).Row(0)
            sample(2) = dst1(roi1).Row(roi1.Height - 1)
            sample(4) = dst1(roi1).Col(0)
            sample(6) = dst1(roi1).Col(roi1.Width - 1)
            Dim nextFitList As New List(Of fit)
            For j = 0 To roilist.Count - 1
                If roiIndex = j Then Continue For
                Dim roi2 = roilist(j)
                sample(1) = dst1(roi2).Row(roi1.Height - 1)
                sample(3) = dst1(roi2).Row(0)
                sample(5) = dst1(roi2).Col(roi2.Width - 1)
                sample(7) = dst1(roi2).Col(0)

                Dim absDiff() = computeMetric(sample)
                For k = 0 To maxDiff.Count - 1
                    If maxDiff(k) < absDiff(k) Then maxDiff(k) = absDiff(k)
                Next

                nextFitList.Add(New fit(absDiff, roiIndex, j))
            Next

            Dim sortedUp As New SortedList(Of Single, fit)(New CompareSingle)
            Dim sortedDn As New SortedList(Of Single, fit)(New CompareSingle)
            Dim sortedLt As New SortedList(Of Single, fit)(New CompareSingle)
            Dim sortedRt As New SortedList(Of Single, fit)(New CompareSingle)
            For j = 0 To nextFitList.Count - 1
                nextFitList.ElementAt(j).metricUp = (maxDiff(0) - nextFitList.ElementAt(j).metricUp) / maxDiff(0)
                nextFitList.ElementAt(j).metricDn = (maxDiff(1) - nextFitList.ElementAt(j).metricDn) / maxDiff(1)
                nextFitList.ElementAt(j).metricLt = (maxDiff(2) - nextFitList.ElementAt(j).metricLt) / maxDiff(2)
                nextFitList.ElementAt(j).metricRt = (maxDiff(3) - nextFitList.ElementAt(j).metricRt) / maxDiff(3)
                sortedUp.Add(nextFitList.ElementAt(j).metricUp, nextFitList.ElementAt(j))
                sortedDn.Add(nextFitList.ElementAt(j).metricDn, nextFitList.ElementAt(j))
                sortedLt.Add(nextFitList.ElementAt(j).metricLt, nextFitList.ElementAt(j))
                sortedRt.Add(nextFitList.ElementAt(j).metricRt, nextFitList.ElementAt(j))
            Next
            nextFitList.Clear()
            Dim bestUp As New List(Of Integer)
            Dim bestDn As New List(Of Integer)
            Dim bestLt As New List(Of Integer)
            Dim bestRt As New List(Of Integer)
            For i = 0 To sortedUp.Count - 1
                bestUp.Add(sortedUp.ElementAt(i).Value.neighbor)
                bestDn.Add(sortedDn.ElementAt(i).Value.neighbor)
                bestLt.Add(sortedLt.ElementAt(i).Value.neighbor)
                bestRt.Add(sortedRt.ElementAt(i).Value.neighbor)
            Next
            Dim bestMetricUp = sortedUp.ElementAt(0).Value.metricUp
            Dim bestMetricDn = sortedDn.ElementAt(0).Value.metricDn
            Dim bestMetricLt = sortedLt.ElementAt(0).Value.metricLt
            Dim bestMetricRt = sortedRt.ElementAt(0).Value.metricRt

            Dim Fit As New bestFit
            Fit.AvgMetric = (bestMetricUp + bestMetricDn + bestMetricLt + bestMetricRt) / 4
            Fit.bestUp = bestUp
            Fit.bestDn = bestDn
            Fit.bestLt = bestLt
            Fit.bestRt = bestRt
            Fit.bestMetricUp = bestMetricUp
            Fit.bestMetricDn = bestMetricDn
            Fit.bestMetricLt = bestMetricLt
            Fit.bestMetricRt = bestMetricRt
            Fit.edge = tileSide.none
            Fit.corner = cornerType.none
            Dim minVal As Single = Single.MaxValue, maxVal As Single = Single.MinValue
            Dim maxBestIndex = 0
            For i = 0 To 4 - 1
                Dim nextBest = Choose(i + 1, bestMetricUp, bestMetricDn, bestMetricLt, bestMetricRt)
                If nextBest < minVal Then minVal = nextBest
                If nextBest > maxVal Then
                    maxVal = nextBest
                    maxBestIndex = Choose(i + 1, bestUp.ElementAt(0), bestDn.ElementAt(0), bestLt.ElementAt(0), bestRt.ElementAt(0))
                End If
            Next
            Fit.MinBest = minVal
            Fit.maxBest = maxVal
            Fit.maxBestIndex = maxBestIndex
            Fit.index = roiIndex

            Dim belowAvg As Integer = 0
            For j = 0 To 4 - 1
                Dim nextBest = Choose(j + 1, Fit.bestMetricUp, Fit.bestMetricDn, Fit.bestMetricLt, Fit.bestMetricRt)
                If Fit.AvgMetric > nextBest Then
                    belowAvg += 1
                End If
            Next
            If belowAvg = 2 Then
                Fit.corner = getCornerType(Fit)
                sortedCorners.Add(Fit.maxBest - Fit.MinBest, Fit)
            End If
            If belowAvg = 1 Then Fit.edge = getEdgeType(Fit)
            fitlist.Add(Fit)
        Next

        For i = 0 To sortedCorners.Count - 1
            corners.Add(sortedCorners.ElementAt(i).Value)
        Next
        Return corners
    End Function
End Module





' https://github.com/nemanja-m/gaps
Public Class Puzzle_Basics
    Inherits VBparent
    Public grid As Thread_Grid
    Public scrambled As New List(Of cv.Rect) ' this is every roi regardless of size.
    Public unscrambled As New List(Of cv.Rect) ' this is every roi regardless of size.
    Public restartRequested As Boolean
    Dim gridWidthSlider As System.Windows.Forms.TrackBar
    Dim gridHeightSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        initParent()
        grid = New Thread_Grid
        gridWidthSlider = findSlider("ThreadGrid Width")
        gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = src.Cols / 10
        gridHeightSlider.Value = src.Rows / 8

        grid.Run()
        task.desc = "Create the puzzle pieces for toy genetic or annealing algorithm."
    End Sub
    Function Shuffle(Of T)(collection As IEnumerable(Of T)) As List(Of T)
        Dim r As Random = New Random()
        Shuffle = collection.OrderBy(Function(a) r.Next()).ToList()
    End Function
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static width As Integer
        Static height As Integer
        If width <> gridWidthSlider.Value Or height <> gridHeightSlider.Value Or task.frameCount = 0 Or restartRequested Then
            restartRequested = False
            grid.Run()
            width = grid.roiList(0).Width
            height = grid.roiList(0).Height

            unscrambled.Clear()
            Dim inputROI As New List(Of cv.Rect)
            For j = 0 To grid.roiList.Count - 1
                Dim roi = grid.roiList(j)
                If roi.Width = width And roi.Height = height Then
                    inputROI.Add(grid.roiList(j))
                    unscrambled.Add(grid.roiList(j))
                End If
            Next

            scrambled = Shuffle(inputROI)
        End If

        ' display image with shuffled roi's
        For i = 0 To scrambled.Count - 1
            Dim roi = grid.roiList(i)
            Dim roi2 = scrambled(i)
            If roi.Width = width And roi.Height = height And roi2.Width = width And roi2.Height = height Then dst1(roi2) = src(roi)
        Next
    End Sub
End Class





Public Class Puzzle_Solver
    Inherits VBparent
    Dim puzzle As Puzzle_Basics
    Public roilist() As cv.Rect
    Dim usedList As New List(Of Integer)
    Dim fitlist As New List(Of bestFit)
    Public Sub New()
        initParent()
        puzzle = New Puzzle_Basics()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 3)
            radio.check(0).Text = "256x180 tile - Easy Puzzle"
            radio.check(1).Text = "128x90  tile - Medium Puzzle"
            radio.check(2).Text = "64x90   tile - Hard Puzzle"
            radio.check(0).Checked = True
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 3)
            check.Box(0).Text = "Reshuffle pieces"
            check.Box(1).Text = "Show poor correlation coefficients"
            check.Box(2).Text = "Clean display (no grid or correlations)"
            check.Box(0).Checked = True
            check.Box(1).Checked = False
        End If
        task.desc = "Put the puzzle back together using the absDiff of the up, down, left and right sides of each ROI."
    End Sub
    Private Function checkUsedList(best As List(Of Integer)) As bestFit
        Dim bfit As New bestFit
        For i = 0 To best.Count - 1
            bfit = fitlist.ElementAt(best.ElementAt(i))
            If usedList.Contains(bfit.index) = False Then Exit For
        Next
        Return bfit
    End Function
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me

        If src.Width = 640 Then
            task.trueText("This algorithm was not setup to work at 640x480.  It works only at 1280x720")
            Exit Sub
        End If
        If task.frameCount = 0 Then
            If src.Width = 640 Then ' must be an even multiple
                radio.check(0).Enabled = False
                radio.check(1).Enabled = False
                radio.check(2).Enabled = False
                radio.check(3).Checked = True
            End If
        End If

        Static saveWidth As Integer
        If src.Height = 180 Then ' can't support the smaller tiles at the low resolution.
            radio.check(1).Enabled = False
            radio.check(2).Enabled = False
            radio.check(0).Checked = True
        End If
        If src.Width <> saveWidth Then
            check.Box(0).Checked = True
            saveWidth = src.Width
        End If
        Dim radioIndex As Integer
        Static frm = findfrm("Puzzle_Solver Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                radioIndex = i
                Exit For
            End If
        Next
        Static saveRadioIndex As Integer
        Static saveResolutionWidth As Integer
        Static xxOffset As Integer
        Static xyOffset As Integer
        Static yxOffset As Integer
        Static yyOffset As Integer
        If check.Box(0).Checked Or task.parms.testAllRunning Or saveRadioIndex <> radioIndex Or saveResolutionWidth <> src.Width Then
            Dim factor = 1
            saveRadioIndex = radioIndex
            saveResolutionWidth = src.Width
            Select Case src.Height
                Case 180
                    factor = 4
                    task.fontSize = 0.4
                Case 360
                    factor = 2
                    task.fontSize = 0.7
                Case 720
                    factor = 1
                    task.fontSize = 1.8
            End Select
            If radio.check(0).Checked Then
                puzzle.grid.sliders.trackbar(0).Value = 256 / factor
                puzzle.grid.sliders.trackbar(1).Value = 180 / factor
                task.fontSize /= 2
                xxOffset = puzzle.grid.sliders.trackbar(0).Value / 2
                yxOffset = puzzle.grid.sliders.trackbar(0).Value * 3 / 4
            ElseIf radio.check(1).Checked Then
                puzzle.grid.sliders.trackbar(0).Value = 128 / factor
                puzzle.grid.sliders.trackbar(1).Value = 90 / factor
                task.fontSize /= 2
                xxOffset = puzzle.grid.sliders.trackbar(0).Value / 3
                yxOffset = puzzle.grid.sliders.trackbar(0).Value * 3 / 4
            ElseIf radio.check(2).Checked Then
                puzzle.grid.sliders.trackbar(0).Value = 64 / factor
                puzzle.grid.sliders.trackbar(1).Value = 90 / factor
                task.fontSize /= 2
                xxOffset = puzzle.grid.sliders.trackbar(0).Value / 4
                yxOffset = puzzle.grid.sliders.trackbar(0).Value / 2
            Else
                puzzle.grid.sliders.trackbar(0).Value = 128 / factor
                puzzle.grid.sliders.trackbar(1).Value = 80 / factor
                task.fontSize /= 2
                xxOffset = puzzle.grid.sliders.trackbar(0).Value / 4
                yxOffset = puzzle.grid.sliders.trackbar(0).Value / 2
            End If
            puzzle.grid.src = src
            puzzle.grid.Run()
            xyOffset = puzzle.grid.sliders.trackbar(1).Value * 9 / 10
            yyOffset = puzzle.grid.sliders.trackbar(1).Value / 2
            puzzle.restartRequested = True
            puzzle.src = src
            puzzle.Run()
            roilist = puzzle.grid.roiList.ToArray
        End If

        dst1 = puzzle.dst1
        Dim cornerlist = fitCheck(dst1, roilist, fitlist)

        If cornerlist.Count = 0 Then Exit Sub ' rare condition, found no corner tiles.  How is this possible?
        Dim bestCorner = cornerlist.ElementAt(0)
        For i = 0 To cornerlist.Count - 1
            bestCorner = cornerlist.ElementAt(i)
            If bestCorner.corner <> cornerType.none Then Exit For
        Next
        Dim fit = bestCorner
        Dim roi = roilist(fit.index)
        Dim startcorner = bestCorner.corner

        Dim col As Integer
        Dim cols = CInt(src.Width / roilist(0).Width)

        Select Case bestCorner.corner
            Case cornerType.upperLeft, cornerType.upperRight
                For nexty = 0 To dst2.Height - 1 Step roi.Height
                    For nextx = 0 To dst2.Width - 1 Step roi.Width
                        dst1(roi).CopyTo(dst2(New cv.Rect(nextx, nexty, roi.Width, roi.Height)))
                        usedList.Add(fit.index)
                        col += 1
                        If col < cols Then
                            If startcorner = cornerType.upperLeft Then
                                fit = checkUsedList(fit.bestRt)
                            Else
                                fit = checkUsedList(fit.bestLt)
                            End If
                            roi = roilist(fit.index)
                        End If
                    Next
                    col = 0
                    fit = checkUsedList(bestCorner.bestDn)
                    roi = roilist(fit.index)
                    bestCorner = fit
                Next
            Case cornerType.lowerLeft, cornerType.lowerRight
                For nexty = dst2.Height - roi.Height To 0 Step -roi.Height
                    For nextx = 0 To dst2.Width - 1 Step roi.Width
                        dst1(roi).CopyTo(dst2(New cv.Rect(nextx, nexty, roi.Width, roi.Height)))
                        usedList.Add(fit.index)
                        col += 1
                        If col < cols Then
                            If startcorner = cornerType.lowerLeft Then
                                fit = checkUsedList(fit.bestRt)
                            Else
                                fit = checkUsedList(fit.bestLt)
                            End If
                            roi = roilist(fit.index)
                        End If
                    Next
                    col = 0
                    fit = checkUsedList(bestCorner.bestUp)
                    roi = roilist(fit.index)
                    bestCorner = fit
                Next
        End Select

        If check.Box(2).Checked = False Then
            ' how well did we do?
            Dim tmp As New cv.Mat
            Dim left As cv.Mat, right As cv.Mat, bottom As cv.Mat, top As cv.Mat
            For y = 0 To dst2.Height - 1 Step roi.Height
                For x = 0 To dst2.Width - 1 Step roi.Width
                    Dim tileRoi = New cv.Rect(x, y, roi.Width, roi.Height)
                    Dim tileRight = New cv.Rect(x + roi.Width, y, roi.Width, roi.Height)
                    Dim tileBelow = New cv.Rect(x, y + roi.Height, roi.Width, roi.Height)
                    If x <> dst2.Width - roi.Width Then
                        right = dst2(tileRoi).Col(roi.Width - 1)
                        left = dst2(tileRight).Col(0)
                        cv.Cv2.MatchTemplate(right, left, tmp, cv.TemplateMatchModes.CCoeffNormed)
                        Dim correlationRight = tmp.Get(Of Single)(0, 0)
                        If check.Box(1).Checked And correlationRight < 0.9 Then
                            cv.Cv2.PutText(dst2, Format(correlationRight, "0.00"), New cv.Point(x + yxOffset, y + yyOffset),
                                       cv.HersheyFonts.HersheySimplex, task.fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                        End If
                    End If

                    If y <> dst2.Height - roi.Height Then
                        bottom = dst2(tileRoi).Row(roi.Height - 1)
                        top = dst2(tileBelow).Row(0)

                        cv.Cv2.MatchTemplate(bottom, top, tmp, cv.TemplateMatchModes.CCoeffNormed)
                        Dim correlationBottom = tmp.Get(Of Single)(0, 0)

                        If check.Box(1).Checked And correlationBottom < 0.9 Then
                            cv.Cv2.PutText(dst2, Format(correlationBottom, "0.00"), New cv.Point(x + xxOffset, y + xyOffset),
                                       cv.HersheyFonts.HersheySimplex, task.fontSize, cv.Scalar.White, 1, cv.LineTypes.AntiAlias)
                        End If
                    End If
                Next
            Next

            dst2.SetTo(cv.Scalar.White, puzzle.grid.gridMask)
        End If

        fitlist.Clear()
        usedList.Clear()
        check.Box(0).Checked = False

        label1 = "Input to puzzle solver"
        label2 = If(check.Box(1).Checked, "Poor correlations shown (ambiguities possible)", "Solution (ambiguities possible)")
        If radio.check(1).Checked Or radio.check(2).Checked Then Thread.Sleep(1000)
    End Sub
End Class



