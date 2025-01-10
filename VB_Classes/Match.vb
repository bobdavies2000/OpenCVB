Imports cv = OpenCvSharp
Imports System.Threading
Imports System.Windows.Forms
Public Class Match_Basics : Inherits TaskParent
    Public options As New Options_Features

    Public template As cv.Mat ' Provide this
    Public searchRect As New cv.Rect ' Provide this 

    Public mmData As mmData
    Public correlation As Single ' Resulting Correlation coefficient
    Public matchCenter As cv.Point
    Public matchRect As New cv.Rect
    Public Sub New()
        If standalone Then task.gOptions.debugChecked = True
        labels(2) = If(standaloneTest(), "Draw anywhere to define a new target", "Both drawRect must be provided by the caller.")
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        desc = "Find the requested template in an image.  Managing template is responsibility of caller (allows multiple targets per image.)"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()
        If standalone Then
            If task.gOptions.debugChecked Then
                task.gOptions.debugChecked = False
                Dim inputRect = If(task.firstPass, New cv.Rect(25, 25, 25, 25), ValidateRect(task.drawRect))
                template = src(inputRect)
            End If
        End If

        If searchRect.Width = 0 Then
            cv.Cv2.MatchTemplate(template, src, dst0, options.matchOption)
        Else
            cv.Cv2.MatchTemplate(template, src(searchRect), dst0, options.matchOption)
        End If
        mmData = GetMinMax(dst0)

        correlation = mmData.maxVal
        labels(2) = "Correlation = " + Format(correlation, "#,##0.000")
        Dim w = template.Width, h = template.Height
        If searchRect.Width = 0 Then
            matchCenter = New cv.Point(mmData.maxLoc.X + w / 2, mmData.maxLoc.Y + h / 2)
            matchRect = New cv.Rect(mmData.maxLoc.X, mmData.maxLoc.Y, w, h)
        Else
            matchCenter = New cv.Point(searchRect.X + mmData.maxLoc.X + w / 2, searchRect.Y + mmData.maxLoc.Y + h / 2)
            matchRect = New cv.Rect(searchRect.X + mmData.maxLoc.X, searchRect.Y + mmData.maxLoc.Y, w, h)
        End If
        If standalone Then
            dst2 = src
            DrawCircle(dst2, matchCenter, task.DotSize, white)
            dst3 = dst0.Normalize(0, 255, cv.NormTypes.MinMax)
        End If
    End Sub
End Class









Public Class Match_BasicsTest : Inherits TaskParent
    Public match As New Match_Basics
    Public Sub New()
        labels = {"", "", "Draw a rectangle to be tracked", "Highest probability of a match at the brightest point below"}
        desc = "Test the Match_Basics algorithm"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If (task.firstPass Or (task.mouseClickFlag And task.drawRect.Width <> 0)) And standaloneTest() Then
            Dim r = If(task.firstPass, New cv.Rect(25, 25, 25, 25), ValidateRect(task.drawRect))
            match.template = src(r)
            task.drawRectClear = True
        End If

        match.Run(src)

        If standaloneTest() Then
            dst2 = src
            DrawCircle(dst2, match.matchCenter, task.DotSize, white)
            dst3 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            SetTrueText(Format(match.correlation, fmt3), match.matchCenter)
        End If
    End Sub
End Class








Public Class Match_RandomTest : Inherits TaskParent
    Dim flow As New Font_FlowText
    Public template As cv.Mat
    Public correlationMat As New cv.Mat
    Public correlation As Single
    Public mm As mmData
    Public minCorrelation = Single.MaxValue
    Public maxCorrelation = Single.MinValue
    Public options As New Options_Features
    Public Sub New()
        flow.parentData = Me
        desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()
        If standaloneTest() Then
            Static saveSampleCount = options.featurePoints
            If saveSampleCount <> options.featurePoints Then
                saveSampleCount = options.featurePoints
                maxCorrelation = Single.MinValue
                minCorrelation = Single.MaxValue
            End If
            template = New cv.Mat(New cv.Size(options.featurePoints, 1), cv.MatType.CV_32FC1)
            src = New cv.Mat(New cv.Size(options.featurePoints, 1), cv.MatType.CV_32FC1)
            cv.Cv2.Randn(template, 100, 25)
            cv.Cv2.Randn(src, 0, 25)
        End If

        cv.Cv2.MatchTemplate(template, src, correlationMat, options.matchOption)
        mm = GetMinMax(correlationMat)
        mm.maxLoc = New cv.Point(mm.maxLoc.X + template.Width / 2, mm.maxLoc.Y + template.Height / 2)
        correlation = mm.maxVal
        If correlation < minCorrelation Then minCorrelation = correlation
        If correlation > maxCorrelation Then maxCorrelation = correlation
        labels(2) = "Correlation = " + Format(correlation, "#,##0.000")
        If standaloneTest() Then
            dst2.SetTo(0)
            labels(2) = options.matchText + " for " + CStr(template.Cols) + " random test samples = " + Format(correlation, "#,##0.00")
            flow.nextMsg = options.matchText + " = " + Format(correlation, "#,##0.00")
            flow.Run(src)
            SetTrueText("The expectation is that the " + CStr(template.Cols) + " random test samples should produce" + vbCrLf +
                        " a correlation coefficient near zero" + vbCrLf +
                        "The larger the sample size, the closer to zero the correlation will be - See 'Sample Size' slider nearby." + vbCrLf +
                        "There should also be symmetry in the min and max around zero." + vbCrLf + vbCrLf +
                        "Min Correlation = " + Format(minCorrelation, fmt3) + vbCrLf +
                        "Max Correlation = " + Format(maxCorrelation, fmt3), 3)
        End If
    End Sub
End Class










Public Class Match_BestEntropy : Inherits TaskParent
    Dim entropy As New Entropy_Highest
    Dim match As New Match_DrawRect
    Public Sub New()
        match.showOutput = True
        labels(2) = "Probabilities that the template matches image"
        labels(3) = "Red is the best template to match (highest entropy)"
        desc = "Track an object - one with the highest entropy - using OpenCV's matchtemplate."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If task.heartBeat Then
            entropy.Run(src)
            task.drawRect = entropy.eMaxRect
        End If
        match.Run(src)
        dst2 = match.dst2
        dst3 = match.dst3
        dst2.SetTo(white, task.gridMask)
    End Sub
End Class












Public Class Match_Motion : Inherits TaskParent
    Dim options As New Options_Features
    Public mask As cv.Mat
    Dim optionsMatch As New Options_Match
    Dim correlationSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        correlationSlider = optiBase.FindSlider("Feature Correlation Threshold")
        mask = New cv.Mat(dst2.Size(), cv.MatType.CV_8U)
        dst3 = mask.Clone
        desc = "Assign each segment a correlation coefficient and stdev to the previous frame"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        options.RunOpt()
        optionsMatch.RunOpt()
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)

        dst2 = src.Clone
        If dst2.Channels() = 3 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = dst2.Clone()
        Dim saveFrame As cv.Mat = dst2.Clone
        Dim updateCount As Integer
        mask.SetTo(0)

        'Parallel.ForEach(task.gridRects,
        'Sub(roi)
        For Each roi In task.gridRects
            Dim correlation As New cv.Mat, mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(dst2(roi), mean, stdev)
            If stdev > optionsMatch.stdevThreshold Then
                cv.Cv2.MatchTemplate(dst2(roi), lastFrame(roi), correlation, options.matchOption)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                Else
                    mask(roi).SetTo(255)
                    dst2(roi).SetTo(0)
                End If
                SetTrueText(Format(correlation.Get(Of Single)(0, 0), fmt2), pt, 2)
            Else
                Interlocked.Increment(updateCount)
            End If
        Next
        'End Sub)
        dst2.SetTo(255, task.gridMask)
        dst3.SetTo(0)
        saveFrame.CopyTo(dst3, mask)
        lastFrame = saveFrame
        Dim corrPercent = Format(correlationSlider.Value / 100, "0.0%") + " correlation"
        labels(2) = "Correlation value for each cell is shown. " + CStr(updateCount) + " of " + CStr(task.gridRects.Count) + " with < " + corrPercent +
                    " or stdev < " + Format(optionsMatch.stdevThreshold, fmt0)
        labels(3) = CStr(task.gridRects.Count - updateCount) + " segments out of " + CStr(task.gridRects.Count) + " had > " + corrPercent
    End Sub
End Class










Public Class Match_Lines : Inherits TaskParent
    Dim knn As New KNN_N4Basics
    Dim lines As New Line_Basics
    Public Sub New()
        labels(2) = "This is not matching lines from the previous frame because lines often disappear and nearby lines are selected."
        desc = "Use the 2 points from a line as input to a 4-dimension KNN"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        Static lastPt As New List(Of linePoints)(task.lpList)

        knn.queries.Clear()
        For Each lp In task.lpList
            knn.queries.Add(New cv.Vec4f(lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y))
        Next
        If task.optionsChanged Then knn.trainInput = New List(Of cv.Vec4f)(knn.queries)
        knn.Run(src)

        If knn.queries.Count = 0 Then Exit Sub

        For Each i In knn.result
            If i >= task.lpList.Count Then Continue For
            Dim lp = task.lpList(i)

            Dim index = knn.result(i, 0)
            If index >= 0 And index < lastPt.Count Then
                Dim lastMP = lastPt(index)
                DrawLine(dst2, lp.p1, lastMP.p2, cv.Scalar.Red)
            End If
        Next

        knn.trainInput = New List(Of cv.Vec4f)(knn.queries)
        lastPt = New List(Of linePoints)(task.lpList)
    End Sub
End Class






Public Class Match_PointSlope : Inherits TaskParent
    Dim lines As New Line_PointSlope
    Dim updateLines As Boolean = True
    Public matches As New List(Of matchRect)
    Dim templates As New List(Of cv.Mat)
    Dim mats As New Mat_4to1
    Dim strOut1 As String
    Dim strOut2 As String
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        labels = {"", "Output of Lines_PointSlope", "Matched lines", "correlationMats"}
        desc = "Initialize with the best lines in the image and track them using matchTemplate.  Reinitialize when correlations drop."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        dst2 = src.Clone
        Dim sz = task.gridSize

        If updateLines Then
            updateLines = False
            templates.Clear()
            lines.Run(src)
            dst1 = src.Clone
            For Each pts In lines.bestLines
                Dim rect = ValidateRect(New cv.Rect(pts.p1.X - sz, pts.p1.Y - sz, sz * 2, sz * 2))
                templates.Add(src(rect))
                dst1.Rectangle(rect, white, task.lineWidth)

                rect = ValidateRect(New cv.Rect(pts.p2.X - sz, pts.p2.Y - sz, sz * 2, sz * 2))
                templates.Add(src(rect))
                dst1.Rectangle(rect, white, task.lineWidth)

                DrawLine(dst1, pts.p1, pts.p2, task.HighlightColor)
            Next
        End If

        Dim correlationMat As New cv.Mat
        Dim mm As mmData
        matches.Clear()
        Dim newTemplates As New List(Of cv.Mat)
        For i = 0 To lines.bestLines.Count - 1
            Dim ptS = lines.bestLines(i)
            Dim mr As matchRect
            For j = 0 To 1
                Dim pt = Choose(j + 1, ptS.p1, ptS.p2)
                cv.Cv2.MatchTemplate(templates(i * 2 + j), src, correlationMat, cv.TemplateMatchModes.CCoeffNormed)

                mm = GetMinMax(correlationMat)

                If i < 4 Then ' only 4 mats can be displayed in the Mat_4to1 algorithm...
                    mats.mat(i).SetTo(0)
                    correlationMat = Convert32f_To_8UC3(correlationMat)
                    Dim r = New cv.Rect((dst2.Width - correlationMat.Width) / 2, (dst2.Height - correlationMat.Height) / 2, correlationMat.Width, correlationMat.Height)
                    correlationMat.CopyTo(mats.mat(i)(r))
                End If

                If j = 0 Then
                    mr.p1 = New cv.Point(mm.maxLoc.X + sz, mm.maxLoc.Y + sz)
                    mr.correlation1 = mm.maxVal
                    Dim rect = ValidateRect(New cv.Rect(mr.p1.X - sz, mr.p1.Y - sz, sz * 2, sz * 2))
                    newTemplates.Add(src(rect))
                Else
                    mr.p2 = New cv.Point(mm.maxLoc.X + sz, mm.maxLoc.Y + sz)
                    mr.correlation2 = mm.maxVal
                    Dim rect = ValidateRect(New cv.Rect(mr.p2.X - sz, mr.p2.Y - sz, sz * 2, sz * 2))
                    newTemplates.Add(src(rect))
                End If
            Next
            matches.Add(mr)
        Next

        ' templates = New List(Of cv.Mat)(newTemplates)
        mats.Run(src)
        dst3 = mats.dst2

        Dim incorrectCount As Integer
        For Each mr In matches
            If mr.correlation1 < 0.5 Or mr.correlation2 < 0.5 Then incorrectCount += 1
            DrawLine(dst2, mr.p1, mr.p2, task.HighlightColor)
            DrawCircle(dst2, mr.p1, task.DotSize, task.HighlightColor)
            DrawCircle(dst2, mr.p2, task.DotSize, task.HighlightColor)
            If task.heartBeat Then
                strOut1 = Format(mr.correlation1, fmt3)
                strOut2 = Format(mr.correlation2, fmt3)
            End If
            SetTrueText(strOut1, mr.p1, 2)
            SetTrueText(strOut2, mr.p2, 2)
        Next

        labels(2) = CStr(matches.Count - incorrectCount) + " lines were confirmed with correlations"
        If incorrectCount Then updateLines = True
    End Sub
End Class







Public Class Match_TraceRedC : Inherits TaskParent
    Dim frameList As New List(Of cv.Mat)
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size(), cv.MatType.CV_32S, 0)
        dst1 = New cv.Mat(dst1.Size(), cv.MatType.CV_32S, 0)
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Track each RedCloud cell center to highlight zones of RedCloud cell instability.  Look for clusters of points in dst2."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If task.heartBeat Then dst2.SetTo(0)
        getRedColor(src)

        If task.optionsChanged Then frameList.Clear()

        dst0.SetTo(0)
        Dim points As New List(Of cv.Point)

        For Each rc In task.redCells
            dst0.Set(Of Byte)(rc.maxDist.Y, rc.maxDist.X, 1)
        Next
        labels(2) = CStr(task.redCells.Count) + " cells added"

        frameList.Add(dst0.Clone)
        If frameList.Count >= task.frameHistoryCount Then
            dst1 = dst1.Subtract(frameList(0))
            frameList.RemoveAt(0)
        End If
        dst1 = dst1.Add(dst0)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst3 = task.redC.dst2
    End Sub
End Class








Public Class Match_DrawRect : Inherits TaskParent
    Dim match As New Match_Basics
    Public inputRect As cv.Rect
    Public showOutput As Boolean
    Public Sub New()
        inputRect = New cv.Rect(dst2.Width / 2 - 20, dst2.Height / 2 - 20, 40, 40) ' arbitrary template to match
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_32F, cv.Scalar.All(0))
        If standalone Then labels(3) = "Probabilities (draw rectangle to test again)"
        labels(2) = "Red dot marks best match for the selected region.  Draw a rectangle anywhere to test again. "
        desc = "Find the requested template in task.drawrect in an image"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        Static lastImage As cv.Mat = src.Clone
        If task.mouseClickFlag And task.drawRect.Width <> 0 Then
            inputRect = ValidateRect(task.drawRect)
            match.template = src(inputRect).Clone()
        Else
            If task.firstPass Then match.template = lastImage(inputRect).Clone()
        End If

        match.Run(src)

        If standaloneTest() Or showOutput Then
            dst0 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            dst3.SetTo(0)
            dst0.CopyTo(dst3(New cv.Rect(inputRect.Width / 2, inputRect.Height / 2, dst0.Width, dst0.Height)))
            dst3.Rectangle(inputRect, white, task.lineWidth, task.lineType)
            dst2 = src
        End If

        SetTrueText("maxLoc = " + CStr(match.matchCenter.X) + ", " + CStr(match.matchCenter.Y), New cv.Point(1, 1), 3)

        If standaloneTest() Then
            DrawCircle(dst2, match.matchCenter, task.DotSize, cv.Scalar.Red)
            SetTrueText(Format(match.correlation, fmt3), match.matchCenter, 2)
        End If
        lastImage = src
    End Sub
End Class







Public Class Match_tCell : Inherits TaskParent
    Public tCells As New List(Of tCell)
    Dim cellSlider As TrackBar
    Dim options As New Options_Features
    Dim lineDisp As New Line_DisplayInfoOld
    Public Sub New()
        Dim tc As tCell
        tCells.Add(tc)
        cellSlider = optiBase.FindSlider("MatchTemplate Cell Size")
        desc = "Use MatchTemplate to find the new location of the template and update the tc that was provided."
    End Sub
    Public Function createCell(src As cv.Mat, correlation As Single, pt As cv.Point2f) As tCell
        Dim rSize = cellSlider.Value
        Dim tc As tCell

        tc.rect = ValidateRect(New cv.Rect(pt.X - rSize, pt.Y - rSize, rSize * 2, rSize * 2))
        tc.correlation = correlation
        tc.depth = task.pcSplit(2)(tc.rect).Mean(task.depthMask(tc.rect))(0) / 1000
        tc.center = pt
        tc.searchRect = ValidateRect(New cv.Rect(tc.center.X - rSize * 3, tc.center.Y - rSize * 3, rSize * 6, rSize * 6))
        If tc.template Is Nothing Then tc.template = src(tc.rect).Clone
        Return tc
    End Function
    Public Overrides Sub runAlg(src As cv.Mat)
        Dim rSize = cellSlider.Value
        If standaloneTest() And task.heartBeat Then
            options.RunOpt()
            tCells.Clear()
            tCells.Add(createCell(src, 0, New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))))
            tCells.Add(createCell(src, 0, New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))))
        End If

        For i = 0 To tCells.Count - 1
            Dim tc = tCells(i)
            Dim input = src(tc.searchRect)
            cv.Cv2.MatchTemplate(tc.template, input, dst0, cv.TemplateMatchModes.CCoeffNormed)
            Dim mm As mmData = GetMinMax(dst0)
            tc.center = New cv.Point2f(tc.searchRect.X + mm.maxLoc.X + rSize, tc.searchRect.Y + mm.maxLoc.Y + rSize)
            tc.searchRect = ValidateRect(New cv.Rect(tc.center.X - rSize * 3, tc.center.Y - rSize * 3, rSize * 6, rSize * 6))
            tc.rect = ValidateRect(New cv.Rect(tc.center.X - rSize, tc.center.Y - rSize, rSize * 2, rSize * 2))
            tc.correlation = mm.maxVal
            tc.depth = task.pcSplit(2)(tc.rect).Mean(task.depthMask(tc.rect))(0) / 1000
            tc.strOut = Format(tc.correlation, fmt2) + vbCrLf + Format(tc.depth, fmt2) + "m"
            tCells(i) = tc
        Next

        If standaloneTest() Then
            lineDisp.tcells = tCells
            lineDisp.Run(src)
            dst2 = lineDisp.dst2
        End If
    End Sub
End Class








Public Class Match_LinePairTest : Inherits TaskParent
    Public ptx(2 - 1) As cv.Point2f
    Public target(ptx.Count - 1) As cv.Mat
    Public correlation(ptx.Count - 1)
    Dim options As New Options_Features
    Public Sub New()
        desc = "Use MatchTemplate to find the new location of the template and update the tc that was provided."
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        Static cellSlider = optiBase.FindSlider("MatchTemplate Cell Size")
        Static corrSlider = optiBase.FindSlider("Feature Correlation Threshold")
        Dim minCorrelation = corrSlider.Value / 100
        Dim rSize = cellSlider.Value
        Dim radius = rSize / 2

        Dim rect As cv.Rect

        options.RunOpt()

        If (target(0) IsNot Nothing And correlation(0) < minCorrelation) Then target(0) = Nothing
        If task.mouseClickFlag Then
            ptx(0) = task.ClickPoint
            ptx(1) = New cv.Point2f(msRNG.Next(rSize, dst2.Width - 2 * rSize), msRNG.Next(rSize, dst2.Height - 2 * rSize))

            rect = ValidateRect(New cv.Rect(ptx(0).X - radius, ptx(0).Y - radius, rSize, rSize))
            target(0) = src(rect)

            rect = ValidateRect(New cv.Rect(ptx(1).X - radius, ptx(1).Y - radius, rSize, rSize))
            target(1) = src(rect)
        End If

        If target(0) Is Nothing Or target(1) Is Nothing Then
            dst3 = src
            SetTrueText("Click anywhere in the image to start the algorithm.")
            Exit Sub
        End If

        dst3 = src.Clone
        dst2 = New cv.Mat(dst2.Size(), cv.MatType.CV_32FC1, 0)

        For i = 0 To ptx.Count - 1
            rect = ValidateRect(New cv.Rect(ptx(i).X - radius, ptx(i).Y - radius, rSize, rSize))
            Dim searchRect = ValidateRect(New cv.Rect(rect.X - rSize, rect.Y - rSize, rSize * 3, rSize * 3))
            cv.Cv2.MatchTemplate(target(i), src(searchRect), dst0, cv.TemplateMatchModes.CCoeffNormed)
            Dim mmData = GetMinMax(dst0)
            correlation(i) = mmData.maxVal
            If i = 0 Then
                dst0.CopyTo(dst2(New cv.Rect(0, 0, dst0.Width, dst0.Height)))
                dst2 = dst2.Threshold(minCorrelation, 255, cv.ThresholdTypes.Binary)
            End If
            ptx(i) = New cv.Point2f(mmData.maxLoc.X + searchRect.X + radius, mmData.maxLoc.Y + searchRect.Y + radius)
            DrawCircle(dst3, ptx(i), task.DotSize, task.HighlightColor)
            dst3.Rectangle(searchRect, cv.Scalar.Yellow, 1)
            rect = ValidateRect(New cv.Rect(ptx(i).X - radius, ptx(i).Y - radius, rSize, rSize))
            target(i) = task.color(rect)
        Next

        labels(3) = "p1 = " + CStr(ptx(0).X) + "," + CStr(ptx(0).Y) + " p2 = " + CStr(ptx(1).X) + "," + CStr(ptx(1).Y)
        labels(2) = "Correlation = " + Format(correlation(0), fmt3) + " Search result is " + CStr(dst0.Width) + "X" + CStr(dst0.Height)
    End Sub
End Class








Public Class Match_GoodFeatureKNN : Inherits TaskParent
    Public knn As New KNN_OneToOne
    Dim frameList As New List(Of cv.Mat)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Maximum travel distance per frame", 1, 20, 5)
        dst0 = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC1, 0)
        dst1 = New cv.Mat(dst2.Size(), cv.MatType.CV_8UC1, 0)
        labels(3) = "Shake camera to see tracking of the highlighted features"
        desc = "Track the GoodFeatures with KNN"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        Static distSlider = optiBase.FindSlider("Maximum travel distance per frame")
        Dim maxDistance = distSlider.Value

        knn.queries = New List(Of cv.Point2f)(task.features)
        knn.Run(src)

        If task.optionsChanged Then
            frameList.Clear()
            dst1.SetTo(0)
        End If

        dst0.SetTo(0)
        For Each lp In knn.matches
            If lp.p1.DistanceTo(lp.p2) <= maxDistance Then dst0.Line(lp.p1, lp.p2, 255, task.lineWidth + 2, cv.LineTypes.Link4)
        Next
        frameList.Add(dst0.Clone)
        If frameList.Count >= task.frameHistoryCount Then
            dst1 = dst1.Subtract(frameList(0))
            frameList.RemoveAt(0)
        End If
        dst1 += dst0
        dst2 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        dst3 = src
        dst3.SetTo(task.HighlightColor, dst2)
    End Sub
End Class







Public Class Match_Point : Inherits TaskParent
    Public pt As cv.Point2f
    Public target As cv.Mat
    Public correlation As Single
    Public radius As Integer
    Public searchRect As cv.Rect
    Dim options As New Options_Features
    Public Sub New()
        labels(2) = "Rectangle shown is the search rectangle."
        desc = "Track the selected point"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If standaloneTest() Then
            SetTrueText("Set the target mat and the pt then run to track an individual point." + vbCrLf +
                        "After running, the pt is updated with the new location and correlation with the updated correlation." + vbCrLf +
                        "There is no output when run standaloneTest()")
            Exit Sub
        End If

        Static cellSlider = optiBase.FindSlider("MatchTemplate Cell Size")
        Dim rSize = cellSlider.Value
        Dim radius = rSize / 2

        Dim rect = ValidateRect(New cv.Rect(pt.X - radius, pt.Y - radius, rSize, rSize))
        searchRect = ValidateRect(New cv.Rect(rect.X - rSize, rect.Y - rSize, rSize * 3, rSize * 3))
        cv.Cv2.MatchTemplate(target(rect), src(searchRect), dst0, cv.TemplateMatchModes.CCoeffNormed)
        Dim mmData = GetMinMax(dst0)
        correlation = mmData.maxVal
        pt = New cv.Point2f(mmData.maxLoc.X + searchRect.X + radius, mmData.maxLoc.Y + searchRect.Y + radius)
        DrawCircle(src, pt, task.DotSize, white)
        src.Rectangle(searchRect, cv.Scalar.Yellow, 1)
    End Sub
End Class








Public Class Match_Points : Inherits TaskParent
    Public ptx As New List(Of cv.Point2f)
    Public correlation As New List(Of Single)
    Public mPoint As New Match_Point
    Public Sub New()
        labels(2) = "Rectangle shown is the search rectangle."
        desc = "Track the selected points"
    End Sub
    Public Overrides Sub runAlg(src As cv.Mat)
        If task.firstPass Then mPoint.target = src.Clone

        If standaloneTest() Then
            ptx = New List(Of cv.Point2f)(task.features)
            SetTrueText("Move camera around to watch the point being tracked", 3)
        End If

        dst2 = src.Clone
        correlation.Clear()
        For i = 0 To ptx.Count - 1
            mPoint.pt = ptx(i)
            mPoint.Run(src)
            correlation.Add(mPoint.correlation)
            ptx(i) = mPoint.pt
            DrawPolkaDot(ptx(i), dst2)
        Next
        mPoint.target = src.Clone
    End Sub
End Class