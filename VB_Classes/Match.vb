Imports cvb = OpenCvSharp
Imports System.Threading
Imports System.Windows.Forms
Public Class Match_Basics : Inherits TaskParent
    Public template As cvb.Mat
    Public mmData As mmData
    Public correlation As Single
    Public options As New Options_Features
    Public matchCenter As cvb.Point
    Public matchRect As New cvb.Rect
    Public searchRect As New cvb.Rect
    Public Sub New()
        If standalone Then task.gOptions.debugChecked = True
        labels(2) = If(standaloneTest(), "Draw anywhere to define a new target", "Both drawRect must be provided by the caller.")
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))
        desc = "Find the requested template in an image.  Managing template is responsibility of caller (allows multiple targets per image.)"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If standalone Then
            If task.gOptions.debugChecked Then
                task.gOptions.debugChecked = False
                Dim inputRect = If(task.FirstPass, New cvb.Rect(25, 25, 25, 25), ValidateRect(task.drawRect))
                template = src(inputRect)
            End If
        End If

        If searchRect.Width = 0 Then
            cvb.Cv2.MatchTemplate(template, src, dst0, options.matchOption)
        Else
            cvb.Cv2.MatchTemplate(template, src(searchRect), dst0, options.matchOption)
        End If
        mmData = GetMinMax(dst0)

        correlation = mmData.maxVal
        labels(2) = "Correlation = " + Format(correlation, "#,##0.000")
        Dim w = template.Width, h = template.Height
        If searchRect.Width = 0 Then
            matchCenter = New cvb.Point(mmData.maxLoc.X + w / 2, mmData.maxLoc.Y + h / 2)
            matchRect = New cvb.Rect(mmData.maxLoc.X, mmData.maxLoc.Y, w, h)
        Else
            matchCenter = New cvb.Point(searchRect.X + mmData.maxLoc.X + w / 2, searchRect.Y + mmData.maxLoc.Y + h / 2)
            matchRect = New cvb.Rect(searchRect.X + mmData.maxLoc.X, searchRect.Y + mmData.maxLoc.Y, w, h)
        End If
        If standalone Then
            dst2 = src
            DrawCircle(dst2, matchCenter, task.DotSize, white)
            dst3 = dst0.Normalize(0, 255, cvb.NormTypes.MinMax)
        End If
    End Sub
End Class









Public Class Match_BasicsTest : Inherits TaskParent
    Public match As New Match_Basics
    Public Sub New()
        labels = {"", "", "Draw a rectangle to be tracked", "Highest probability of a match at the brightest point below"}
        desc = "Test the Match_Basics algorithm"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If (task.FirstPass Or (task.mouseClickFlag And task.drawRect.Width <> 0)) And standaloneTest() Then
            Dim r = If(task.FirstPass, New cvb.Rect(25, 25, 25, 25), ValidateRect(task.drawRect))
            match.template = src(r)
            task.drawRectClear = True
        End If

        match.Run(src)

        If standaloneTest() Then
            dst2 = src
            DrawCircle(dst2,match.matchCenter, task.DotSize, white)
            dst3 = match.dst0.Normalize(0, 255, cvb.NormTypes.MinMax)
            SetTrueText(Format(match.correlation, fmt3), match.matchCenter)
        End If
    End Sub
End Class








Public Class Match_RandomTest : Inherits TaskParent
    Dim flow As New Font_FlowText
    Public template As cvb.Mat
    Public correlationMat As New cvb.Mat
    Public correlation As Single
    Public mm As mmData
    Public minCorrelation = Single.MaxValue
    Public maxCorrelation = Single.MinValue
    Public options As New Options_Features
    Public Sub New()
        flow.parentData = Me
        desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        If standaloneTest() Then
            Static saveSampleCount = options.featurePoints
            If saveSampleCount <> options.featurePoints Then
                saveSampleCount = options.featurePoints
                maxCorrelation = Single.MinValue
                minCorrelation = Single.MaxValue
            End If
            template = New cvb.Mat(New cvb.Size(options.featurePoints, 1), cvb.MatType.CV_32FC1)
            src = New cvb.Mat(New cvb.Size(options.featurePoints, 1), cvb.MatType.CV_32FC1)
            cvb.Cv2.Randn(template, 100, 25)
            cvb.Cv2.Randn(src, 0, 25)
        End If

        cvb.Cv2.MatchTemplate(template, src, correlationMat, options.matchOption)
        mm = GetMinMax(correlationMat)
        mm.maxLoc = New cvb.Point(mm.maxLoc.X + template.Width / 2, mm.maxLoc.Y + template.Height / 2)
        correlation = mm.maxVal
        If correlation < minCorrelation Then minCorrelation = correlation
        If correlation > maxCorrelation Then maxCorrelation = correlation
        labels(2) = "Correlation = " + Format(correlation, "#,##0.000")
        If standaloneTest() Then
            dst2.SetTo(0)
            labels(2) = options.matchText + " for " + CStr(template.Cols) + " random test samples = " + Format(correlation, "#,##0.00")
            flow.nextMsg = options.matchText + " = " + Format(correlation, "#,##0.00")
            flow.Run(empty)
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
    Public Sub RunAlg(src As cvb.Mat)
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
    Public mask As cvb.Mat
    Dim optionsMatch As New Options_Match
    Dim correlationSlider As System.Windows.Forms.TrackBar
    Public Sub New()
        correlationSlider = FindSlider("Feature Correlation Threshold")
        mask = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U)
        dst3 = mask.Clone
        desc = "Assign each segment a correlation coefficient and stdev to the previous frame"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        optionsMatch.RunOpt()
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)

        dst2 = src.Clone
        If dst2.Channels() = 3 Then dst2 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cvb.Mat = dst2.Clone()
        Dim saveFrame As cvb.Mat = dst2.Clone
        Dim updateCount As Integer
        mask.SetTo(0)

        'Parallel.ForEach(task.gridRects,
        'Sub(roi)
        For Each roi In task.gridRects
            Dim correlation As New cvb.Mat, mean As Single, stdev As Single
            cvb.Cv2.MeanStdDev(dst2(roi), mean, stdev)
            If stdev > optionsMatch.stdevThreshold Then
                cvb.Cv2.MatchTemplate(dst2(roi), lastFrame(roi), correlation, options.matchOption)
                Dim pt = New cvb.Point(roi.X + 2, roi.Y + 10)
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
    Dim knn As New KNN_Basics4D
    Dim lines As New Line_Basics
    Public Sub New()
        labels(2) = "This is not matching lines from the previous frame because lines often disappear and nearby lines are selected."
        desc = "Use the 2 points from a line as input to a 4-dimension KNN"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        Static lastPt As New List(Of PointPair)(lines.lpList)

        knn.queries.Clear()
        For Each lp In lines.lpList
            knn.queries.Add(New cvb.Vec4f(lp.p1.X, lp.p1.Y, lp.p2.X, lp.p2.Y))
        Next
        If task.optionsChanged Then knn.trainInput = New List(Of cvb.Vec4f)(knn.queries)
        knn.Run(empty)

        If knn.queries.Count = 0 Then Exit Sub

        For Each i In knn.result
            If i >= lines.lpList.Count Then Continue For
            Dim lp = lines.lpList(i)

            Dim index = knn.result(i, 0)
            If index >= 0 And index < lastPt.Count Then
                Dim lastMP = lastPt(index)
                DrawLine(dst2, lp.p1, lastMP.p2, cvb.Scalar.Red)
            End If
        Next

        knn.trainInput = New List(Of cvb.Vec4f)(knn.queries)
        lastPt = New List(Of PointPair)(lines.lpList)
    End Sub
End Class






Public Class Match_PointSlope : Inherits TaskParent
    Dim lines As New Line_PointSlope
    Dim updateLines As Boolean = True
    Public matches As New List(Of matchRect)
    Dim templates As New List(Of cvb.Mat)
    Dim mats As New Mat_4to1
    Dim strOut1 As String
    Dim strOut2 As String
    Public Sub New()
        If standaloneTest() Then task.gOptions.setDisplay1()
        labels = {"", "Output of Lines_PointSlope", "Matched lines", "correlationMats"}
        desc = "Initialize with the best lines in the image and track them using matchTemplate.  Reinitialize when correlations drop."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone
        Dim w = task.gridSize
        Dim h = w

        If updateLines Then
            updateLines = False
            templates.Clear()
            lines.Run(src)
            dst1 = src.Clone
            For Each pts In lines.bestLines
                Dim rect = ValidateRect(New cvb.Rect(pts.p1.X - w, pts.p1.Y - h, w * 2, h * 2))
                templates.Add(src(rect))
                dst1.Rectangle(rect, white, task.lineWidth)

                rect = ValidateRect(New cvb.Rect(pts.p2.X - w, pts.p2.Y - h, w * 2, h * 2))
                templates.Add(src(rect))
                dst1.Rectangle(rect, white, task.lineWidth)

                DrawLine(dst1, pts.p1, pts.p2, task.HighlightColor)
            Next
        End If

        Dim correlationMat As New cvb.Mat
        Dim mm As mmData
        matches.Clear()
        Dim newTemplates As New List(Of cvb.Mat)
        For i = 0 To lines.bestLines.Count - 1
            Dim ptS = lines.bestLines(i)
            Dim mr As matchRect
            For j = 0 To 1
                Dim pt = Choose(j + 1, ptS.p1, ptS.p2)
                cvb.Cv2.MatchTemplate(templates(i * 2 + j), src, correlationMat, cvb.TemplateMatchModes.CCoeffNormed)

                mm = GetMinMax(correlationMat)

                If i < 4 Then ' only 4 mats can be displayed in the Mat_4to1 algorithm...
                    mats.mat(i).SetTo(0)
                    correlationMat = Convert32f_To_8UC3(correlationMat)
                    Dim r = New cvb.Rect((dst2.Width - correlationMat.Width) / 2, (dst2.Height - correlationMat.Height) / 2, correlationMat.Width, correlationMat.Height)
                    correlationMat.CopyTo(mats.mat(i)(r))
                End If

                If j = 0 Then
                    mr.p1 = New cvb.Point(mm.maxLoc.X + w, mm.maxLoc.Y + h)
                    mr.correlation1 = mm.maxVal
                    Dim rect = ValidateRect(New cvb.Rect(mr.p1.X - w, mr.p1.Y - h, w * 2, h * 2))
                    newTemplates.Add(src(rect))
                Else
                    mr.p2 = New cvb.Point(mm.maxLoc.X + w, mm.maxLoc.Y + h)
                    mr.correlation2 = mm.maxVal
                    Dim rect = ValidateRect(New cvb.Rect(mr.p2.X - w, mr.p2.Y - h, w * 2, h * 2))
                    newTemplates.Add(src(rect))
                End If
            Next
            matches.Add(mr)
        Next

        ' templates = New List(Of cvb.Mat)(newTemplates)
        mats.Run(empty)
        dst3 = mats.dst2

        Dim incorrectCount As Integer
        For Each mr In matches
            If mr.correlation1 < 0.5 Or mr.correlation2 < 0.5 Then incorrectCount += 1
            DrawLine(dst2, mr.p1, mr.p2, task.HighlightColor)
            DrawCircle(dst2,mr.p1, task.DotSize, task.HighlightColor)
            DrawCircle(dst2,mr.p2, task.DotSize, task.HighlightColor)
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
    Dim redC As New RedCloud_Basics
    Dim frameList As New List(Of cvb.Mat)
    Public Sub New()
        dst0 = New cvb.Mat(dst0.Size(), cvb.MatType.CV_32S, 0)
        dst1 = New cvb.Mat(dst1.Size(), cvb.MatType.CV_32S, 0)
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Track each RedCloud cell center to highlight zones of RedCloud cell instability.  Look for clusters of points in dst2."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.heartBeat Or task.cameraStable = False Then dst2.SetTo(0)
        redC.Run(src)

        If task.optionsChanged Then frameList.Clear()

        dst0.SetTo(0)
        Dim points As New List(Of cvb.Point)

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
        dst1.ConvertTo(dst2, cvb.MatType.CV_8U)
        dst2 = dst2.Threshold(0, 255, cvb.ThresholdTypes.Binary)
        dst3 = redC.dst2
    End Sub
End Class








Public Class Match_DrawRect : Inherits TaskParent
    Dim match As New Match_Basics
    Public inputRect As cvb.Rect
    Public showOutput As Boolean
    Public Sub New()
        inputRect = New cvb.Rect(dst2.Width / 2 - 20, dst2.Height / 2 - 20, 40, 40) ' arbitrary template to match
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_32F, cvb.Scalar.All(0))
        If standaloneTest() Then labels(3) = "Probabilities (draw rectangle to test again)"
        labels(2) = "Red dot marks best match for the selected region.  Draw a rectangle anywhere to test again. "
        desc = "Find the requested template in task.drawrect in an image"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static lastImage As cvb.Mat = src.Clone
        If task.mouseClickFlag And task.drawRect.Width <> 0 Then
            inputRect = ValidateRect(task.drawRect)
            match.template = src(inputRect).Clone()
        Else
            If task.FirstPass Then match.template = lastImage(inputRect).Clone()
        End If

        match.Run(src)

        If standaloneTest() Or showOutput Then
            dst0 = match.dst0.Normalize(0, 255, cvb.NormTypes.MinMax)
            dst3.SetTo(0)
            dst0.CopyTo(dst3(New cvb.Rect(inputRect.Width / 2, inputRect.Height / 2, dst0.Width, dst0.Height)))
            dst3.Rectangle(inputRect, white, task.lineWidth, task.lineType)
            dst2 = src
        End If

        SetTrueText("maxLoc = " + CStr(match.matchCenter.X) + ", " + CStr(match.matchCenter.Y), New cvb.Point(1, 1), 3)

        If standaloneTest() Then
            DrawCircle(dst2,match.matchCenter, task.DotSize, cvb.Scalar.Red)
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
        cellSlider = FindSlider("MatchTemplate Cell Size")
        desc = "Use MatchTemplate to find the new location of the template and update the tc that was provided."
    End Sub
    Public Function createCell(src As cvb.Mat, correlation As Single, pt As cvb.Point2f) As tCell
        Dim rSize = cellSlider.Value
        Dim tc As tCell

        tc.rect = ValidateRect(New cvb.Rect(pt.X - rSize, pt.Y - rSize, rSize * 2, rSize * 2))
        tc.correlation = correlation
        tc.depth = task.pcSplit(2)(tc.rect).Mean(task.depthMask(tc.rect))(0) / 1000
        tc.center = pt
        tc.searchRect = ValidateRect(New cvb.Rect(tc.center.X - rSize * 3, tc.center.Y - rSize * 3, rSize * 6, rSize * 6))
        If tc.template Is Nothing Then tc.template = src(tc.rect).Clone
        Return tc
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        Dim rSize = cellSlider.Value
        If standaloneTest() And task.heartBeat Then
            options.RunOpt()
            tCells.Clear()
            tCells.Add(createCell(src, 0, New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))))
            tCells.Add(createCell(src, 0, New cvb.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))))
        End If

        For i = 0 To tCells.Count - 1
            Dim tc = tCells(i)
            Dim input = src(tc.searchRect)
            cvb.Cv2.MatchTemplate(tc.template, input, dst0, cvb.TemplateMatchModes.CCoeffNormed)
            Dim mm as mmData = GetMinMax(dst0)
            tc.center = New cvb.Point2f(tc.searchRect.X + mm.maxLoc.X + rSize, tc.searchRect.Y + mm.maxLoc.Y + rSize)
            tc.searchRect = ValidateRect(New cvb.Rect(tc.center.X - rSize * 3, tc.center.Y - rSize * 3, rSize * 6, rSize * 6))
            tc.rect = ValidateRect(New cvb.Rect(tc.center.X - rSize, tc.center.Y - rSize, rSize * 2, rSize * 2))
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
    Public ptx(2 - 1) As cvb.Point2f
    Public target(ptx.Count - 1) As cvb.Mat
    Public correlation(ptx.Count - 1)
    Dim options As New Options_Features
    Public Sub New()
        desc = "Use MatchTemplate to find the new location of the template and update the tc that was provided."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static cellSlider = FindSlider("MatchTemplate Cell Size")
        Static corrSlider = FindSlider("Feature Correlation Threshold")
        Dim minCorrelation = corrSlider.Value / 100
        Dim rSize = cellSlider.Value
        Dim radius = rSize / 2

        Dim rect As cvb.Rect

        Options.RunOpt()

        If (target(0) IsNot Nothing And correlation(0) < minCorrelation) Then target(0) = Nothing
        If task.mouseClickFlag Then
            ptx(0) = task.ClickPoint
            ptx(1) = New cvb.Point2f(msRNG.Next(rSize, dst2.Width - 2 * rSize), msRNG.Next(rSize, dst2.Height - 2 * rSize))

            rect = ValidateRect(New cvb.Rect(ptx(0).X - radius, ptx(0).Y - radius, rSize, rSize))
            target(0) = src(rect)

            rect = ValidateRect(New cvb.Rect(ptx(1).X - radius, ptx(1).Y - radius, rSize, rSize))
            target(1) = src(rect)
        End If

        If target(0) Is Nothing Or target(1) Is Nothing Then
            dst3 = src
            SetTrueText("Click anywhere in the image to start the algorithm.")
            Exit Sub
        End If

        dst3 = src.Clone
        dst2 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_32FC1, 0)

        For i = 0 To ptx.Count - 1
            rect = ValidateRect(New cvb.Rect(ptx(i).X - radius, ptx(i).Y - radius, rSize, rSize))
            Dim searchRect = ValidateRect(New cvb.Rect(rect.X - rSize, rect.Y - rSize, rSize * 3, rSize * 3))
            cvb.Cv2.MatchTemplate(target(i), src(searchRect), dst0, cvb.TemplateMatchModes.CCoeffNormed)
            Dim mmData = GetMinMax(dst0)
            correlation(i) = mmData.maxVal
            If i = 0 Then
                dst0.CopyTo(dst2(New cvb.Rect(0, 0, dst0.Width, dst0.Height)))
                dst2 = dst2.Threshold(minCorrelation, 255, cvb.ThresholdTypes.Binary)
            End If
            ptx(i) = New cvb.Point2f(mmData.maxLoc.X + searchRect.X + radius, mmData.maxLoc.Y + searchRect.Y + radius)
            DrawCircle(dst3,ptx(i), task.DotSize, task.HighlightColor)
            dst3.Rectangle(searchRect, cvb.Scalar.Yellow, 1)
            rect = ValidateRect(New cvb.Rect(ptx(i).X - radius, ptx(i).Y - radius, rSize, rSize))
            target(i) = task.color(rect)
        Next

        labels(3) = "p1 = " + CStr(ptx(0).X) + "," + CStr(ptx(0).Y) + " p2 = " + CStr(ptx(1).X) + "," + CStr(ptx(1).Y)
        labels(2) = "Correlation = " + Format(correlation(0), fmt3) + " Search result is " + CStr(dst0.Width) + "X" + CStr(dst0.Height)
    End Sub
End Class








Public Class Match_GoodFeatureKNN : Inherits TaskParent
    Public knn As New KNN_NoDups
    Public feat As New Feature_Stable
    Dim frameList As New List(Of cvb.Mat)
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Maximum travel distance per frame", 1, 20, 5)
        dst0 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC1, 0)
        dst1 = New cvb.Mat(dst2.Size(), cvb.MatType.CV_8UC1, 0)
        labels(3) = "Shake camera to see tracking of the highlighted features"
        desc = "Track the GoodFeatures with KNN"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Static distSlider = FindSlider("Maximum travel distance per frame")
        Dim maxDistance = distSlider.Value

        feat.Run(src)

        knn.queries = New List(Of cvb.Point2f)(task.features)
        knn.Run(empty)

        If task.optionsChanged Then
            frameList.Clear()
            dst1.SetTo(0)
        End If

        dst0.SetTo(0)
        For Each mp In knn.matches
            If mp.p1.DistanceTo(mp.p2) <= maxDistance Then dst0.Line(mp.p1, mp.p2, 255, task.lineWidth + 2, cvb.LineTypes.Link4)
        Next
        frameList.Add(dst0.Clone)
        If frameList.Count >= task.frameHistoryCount Then
            dst1 = dst1.Subtract(frameList(0))
            frameList.RemoveAt(0)
        End If
        dst1 += dst0
        dst2 = dst1.Threshold(0, 255, cvb.ThresholdTypes.Binary)

        dst3 = src
        dst3.SetTo(task.HighlightColor, dst2)
    End Sub
End Class







Public Class Match_Point : Inherits TaskParent
    Public pt As cvb.Point2f
    Public target As cvb.Mat
    Public correlation As Single
    Public radius As Integer
    Public searchRect As cvb.Rect
    Dim options As New Options_Features
    Public Sub New()
        labels(2) = "Rectangle shown is the search rectangle."
        desc = "Track the selected point"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If standaloneTest() Then
            SetTrueText("Set the target mat and the pt then run to track an individual point." + vbCrLf +
                        "After running, the pt is updated with the new location and correlation with the updated correlation." + vbCrLf +
                        "There is no output when run standaloneTest()")
            Exit Sub
        End If

        Static cellSlider = FindSlider("MatchTemplate Cell Size")
        Dim rSize = cellSlider.Value
        Dim radius = rSize / 2

        Dim rect = ValidateRect(New cvb.Rect(pt.X - radius, pt.Y - radius, rSize, rSize))
        searchRect = ValidateRect(New cvb.Rect(rect.X - rSize, rect.Y - rSize, rSize * 3, rSize * 3))
        cvb.Cv2.MatchTemplate(target(rect), src(searchRect), dst0, cvb.TemplateMatchModes.CCoeffNormed)
        Dim mmData = GetMinMax(dst0)
        correlation = mmData.maxVal
        pt = New cvb.Point2f(mmData.maxLoc.X + searchRect.X + radius, mmData.maxLoc.Y + searchRect.Y + radius)
        DrawCircle(src, pt, task.DotSize, white)
        src.Rectangle(searchRect, cvb.Scalar.Yellow, 1)
    End Sub
End Class








Public Class Match_Points : Inherits TaskParent
    Public ptx As New List(Of cvb.Point2f)
    Public correlation As New List(Of Single)
    Public mPoint As New Match_Point
    Dim feat As New Feature_Stable
    Public Sub New()
        labels(2) = "Rectangle shown is the search rectangle."
        desc = "Track the selected points"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If task.FirstPass Then mPoint.target = src.Clone

        If standaloneTest() Then
            feat.Run(src)
            ptx = New List(Of cvb.Point2f)(task.features)
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