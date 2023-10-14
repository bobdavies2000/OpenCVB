Imports cv = OpenCvSharp
Imports System.Threading
Imports System.Text.RegularExpressions

Public Class Match_Basics : Inherits VB_Algorithm
    Public drawRect As cv.Rect
    Public template As cv.Mat
    Public mmData As mmData
    Public correlation As Single
    Public options As New Options_FeatureMatch
    Public matchCenter As cv.Point
    Public Sub New()
        labels(2) = If(standalone, "Draw anywhere to define a new target", "Both drawRect must be provided by the caller.")
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32F, 0)
        desc = "Find the requested template in an image.  Managing template is responsibility of caller (allows multiple targets per image.)"
    End Sub
    Public Function displayResults() As cv.Mat
        dst0 = dst0.Normalize(0, 255, cv.NormTypes.MinMax)
        Static lastRect As cv.Rect = drawRect
        If lastRect <> drawRect Then
            dst3.SetTo(0)
            lastRect = drawRect
        End If
        dst0.CopyTo(dst3(New cv.Rect(drawRect.Width / 2, drawRect.Height / 2, dst0.Width, dst0.Height)))
        dst3.Rectangle(drawRect, cv.Scalar.White, task.lineWidth, task.lineType)

        dst2.Circle(matchCenter, task.dotSize, cv.Scalar.White, -1, task.lineType)
        dst3.Circle(matchCenter, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Return dst3
    End Function
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If standalone Then
            setTrueText("This algorithm has no output when run standalone")
            Exit Sub
        End If

        cv.Cv2.MatchTemplate(template, src, dst0, options.matchOption)
        mmData = vbMinMax(dst0)

        correlation = mmData.maxVal
        labels(2) = "Correlation = " + Format(correlation, "#,##0.000")
        matchCenter = New cv.Point(mmData.maxLoc.X + drawRect.Width / 2, mmData.maxLoc.Y + drawRect.Height / 2)
        drawRect = validateRect(New cv.Rect(matchCenter.X - drawRect.Width / 2, matchCenter.Y - drawRect.Height / 2, drawRect.Width, drawRect.Height))
    End Sub
End Class









Public Class Match_BasicsTest : Inherits VB_Algorithm
    Dim match As New Match_Basics
    Public Sub New()
        labels = {"", "", "Draw a rectangle to be tracked", "Highest probability of a match at the brightest point below"}
        desc = "Test the Match_Basics algorithm"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If (firstPass Or (task.mouseClickFlag And task.drawRect.Width <> 0)) And standalone Then
            match.drawRect = If(firstPass, New cv.Rect(25, 25, 25, 25), validateRect(task.drawRect))
            match.template = src(match.drawRect)
            task.drawRectClear = True
        End If

        match.Run(src)

        If standalone Then
            match.dst2 = src
            dst3 = match.displayResults()
            dst2 = match.dst2
            setTrueText(Format(match.correlation, fmt3), match.matchCenter)
        End If
    End Sub
End Class








Public Class Match_RandomTest : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Public template As cv.Mat
    Public correlationMat As New cv.Mat
    Public correlation As Single
    Public mm As mmData
    Public minCorrelation = Single.MaxValue
    Public maxCorrelation = Single.MinValue
    Public options As New Options_Features
    Public Sub New()
        desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()
        If standalone Then
            Static saveSampleCount = options.sampleSize
            If saveSampleCount <> options.sampleSize Then
                saveSampleCount = options.sampleSize
                maxCorrelation = Single.MinValue
                minCorrelation = Single.MaxValue
            End If
            template = New cv.Mat(New cv.Size(CInt(options.sampleSize), 1), cv.MatType.CV_32FC1)
            src = New cv.Mat(New cv.Size(CInt(options.sampleSize), 1), cv.MatType.CV_32FC1)
            cv.Cv2.Randn(template, 100, 25)
            cv.Cv2.Randn(src, 0, 25)
        End If

        cv.Cv2.MatchTemplate(template, src, correlationMat, options.matchOption)
        mm = vbMinMax(correlationMat)
        mm.maxLoc = New cv.Point(mm.maxLoc.X + template.Width / 2, mm.maxLoc.Y + template.Height / 2)
        correlation = mm.maxVal
        If correlation < minCorrelation Then minCorrelation = correlation
        If correlation > maxCorrelation Then maxCorrelation = correlation
        labels(2) = "Correlation = " + Format(correlation, "#,##0.000")
        If standalone Then
            dst2.SetTo(0)
            labels(2) = options.matchText + " for " + CStr(template.Cols) + " random test samples = " + Format(correlation, "#,##0.00")
            flow.msgs.Add(options.matchText + " = " + Format(correlation, "#,##0.00"))
            flow.Run(Nothing)
            setTrueText("The expectation is that the " + CStr(template.Cols) + " random test samples should produce" + vbCrLf +
                        " a correlation coefficient near zero" + vbCrLf +
                        "The larger the sample size, the closer to zero the correlation will be - See 'Sample Size' slider nearby." + vbCrLf +
                        "There should also be symmetry in the min and max around zero." + vbCrLf + vbCrLf +
                        "Min Correlation = " + Format(minCorrelation, fmt3) + vbCrLf +
                        "Max Correlation = " + Format(maxCorrelation, fmt3), 3)
        End If
    End Sub
End Class










Public Class Match_BestEntropy : Inherits VB_Algorithm
    Dim entropy As New Entropy_Highest
    Dim match As New Match_DrawRect
    Public Sub New()
        match.showOutput = True
        labels(2) = "Probabilities that the template matches image"
        labels(3) = "Red is the best template to match (highest entropy)"
        desc = "Track an object - one with the highest entropy - using OpenCV's matchtemplate."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If heartBeat() Then
            entropy.Run(src)
            task.drawRect = entropy.eMaxRect
        End If
        match.Run(src)
        dst2 = match.dst2
        dst3 = match.dst3
        dst2.SetTo(cv.Scalar.White, task.gridMask)
    End Sub
End Class












Public Class Match_Motion : Inherits VB_Algorithm
    Dim options As New Options_FeatureMatch
    Public mask As cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Stdev Threshold", 0, 100, 10)
        mask = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        dst3 = mask.Clone
        desc = "Assign each segment a correlation coefficient and stdev to the previous frame"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        options.RunVB()
        Static stdevSlider = findSlider("Stdev Threshold")
        Static correlationSlider = findSlider("Feature Correlation Threshold")
        Dim stdevThreshold = CSng(stdevSlider.Value)
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)

        dst2 = src.Clone
        If dst2.Channels = 3 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = dst2.Clone()
        Dim saveFrame As cv.Mat = dst2.Clone
        Dim updateCount As Integer
        mask.SetTo(0)

        'Parallel.ForEach(task.gridList,
        'Sub(roi)
        For Each roi In task.gridList
            Dim correlation As New cv.Mat, mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(dst2(roi), mean, stdev)
            If stdev > stdevThreshold Then
                cv.Cv2.MatchTemplate(dst2(roi), lastFrame(roi), correlation, options.matchOption)
                Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                Else
                    mask(roi).SetTo(255)
                    dst2(roi).SetTo(0)
                End If
                setTrueText(Format(correlation.Get(Of Single)(0, 0), fmt2), pt, 2)
            Else
                Interlocked.Increment(updateCount)
            End If
        Next
        'End Sub)
        dst2.SetTo(255, task.gridMask)
        dst3.SetTo(0)
        saveFrame.CopyTo(dst3, mask)
        lastFrame = saveFrame
        Dim corrPercent = Format(correlationSlider.Value / 1000, "0.0%") + " correlation"
        labels(2) = "Correlation value for each cell is shown. " + CStr(updateCount) + " of " + CStr(task.gridList.Count) + " with < " + corrPercent +
                    " or stdev < " + Format(stdevThreshold, fmt0)
        labels(3) = CStr(task.gridList.Count - updateCount) + " segments out of " + CStr(task.gridList.Count) + " had > " + corrPercent
    End Sub
End Class










Public Class Match_Lines : Inherits VB_Algorithm
    Dim knn As New KNN_Basics4D
    Dim lines As New Line_Basics
    Public Sub New()
        labels(2) = "This is not matching lines from the previous frame because lines often disappear and nearby lines are selected."
        desc = "Use the 2 points from a line as input to a 4-dimension KNN"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        lines.Run(src)
        dst2 = lines.dst2
        Static lastPt As New List(Of linePoints)(lines.mpList)

        knn.queries.Clear()
        For Each mps In lines.mpList
            knn.queries.Add(New cv.Vec4f(mps.p1.X, mps.p1.Y, mps.p2.X, mps.p2.Y))
        Next
        If task.optionsChanged Then knn.trainInput = New List(Of cv.Vec4f)(knn.queries)
        knn.Run(Nothing)

        If knn.queries.Count = 0 Then Exit Sub

        For Each i In knn.result
            If i >= lines.mpList.Count Then Continue For
            Dim mps = lines.mpList(i)

            Dim index = knn.result(i, 0)
            If index >= 0 And index < lastPt.Count Then
                Dim lastMP = lastPt(index)
                dst2.Line(mps.p1, lastMP.p2, cv.Scalar.Red, task.lineWidth, task.lineType)
            End If
        Next

        knn.trainInput = New List(Of cv.Vec4f)(knn.queries)
        lastPt = New List(Of linePoints)(lines.mpList)
    End Sub
End Class






Public Class Match_PointSlope : Inherits VB_Algorithm
    Dim lines As New Line_PointSlope
    Dim updateLines As Boolean = True
    Public matches As New List(Of matchRect)
    Dim templates As New List(Of cv.Mat)
    Dim mats As New Mat_4to1
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"", "Output of Lines_PointSlope", "Matched lines", "correlationMats"}
        desc = "Initialize with the best lines in the image and track them using matchTemplate.  Reinitialize when correlations drop."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        dst2 = src.Clone
        Dim w = gOptions.GridSize.Value
        Dim h = w

        If updateLines Then
            updateLines = False
            templates.Clear()
            lines.Run(src)
            dst1 = src.Clone
            For Each pts In lines.bestLines
                Dim rect = validateRect(New cv.Rect(pts.p1.X - w, pts.p1.Y - h, w * 2, h * 2))
                templates.Add(src(rect))
                dst1.Rectangle(rect, cv.Scalar.White, task.lineWidth)

                rect = validateRect(New cv.Rect(pts.p2.X - w, pts.p2.Y - h, w * 2, h * 2))
                templates.Add(src(rect))
                dst1.Rectangle(rect, cv.Scalar.White, task.lineWidth)

                dst1.Line(pts.p1, pts.p2, task.highlightColor, task.lineWidth, task.lineType)
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

                mm = vbMinMax(correlationMat)

                If i < 4 Then ' only 4 mats can be displayed in the Mat_4to1 algorithm...
                    mats.mat(i).SetTo(0)
                    correlationMat = vbNormalize32f(correlationMat)
                    Dim r = New cv.Rect((dst2.Width - correlationMat.Width) / 2, (dst2.Height - correlationMat.Height) / 2, correlationMat.Width, correlationMat.Height)
                    correlationMat.CopyTo(mats.mat(i)(r))
                End If

                If j = 0 Then
                    mr.p1 = New cv.Point(mm.maxLoc.X + w, mm.maxLoc.Y + h)
                    mr.correlation1 = mm.maxVal
                    Dim rect = validateRect(New cv.Rect(mr.p1.X - w, mr.p1.Y - h, w * 2, h * 2))
                    newTemplates.Add(src(rect))
                Else
                    mr.p2 = New cv.Point(mm.maxLoc.X + w, mm.maxLoc.Y + h)
                    mr.correlation2 = mm.maxVal
                    Dim rect = validateRect(New cv.Rect(mr.p2.X - w, mr.p2.Y - h, w * 2, h * 2))
                    newTemplates.Add(src(rect))
                End If
            Next
            matches.Add(mr)
        Next

        ' templates = New List(Of cv.Mat)(newTemplates)
        mats.Run(Nothing)
        dst3 = mats.dst2

        Static strOut1 As String
        Static strOut2 As String
        Dim incorrectCount As Integer
        For Each mr In matches
            If mr.correlation1 < 0.5 Or mr.correlation2 < 0.5 Then incorrectCount += 1
            dst2.Line(mr.p1, mr.p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Circle(mr.p1, task.dotSize, task.highlightColor, -1, task.lineType)
            dst2.Circle(mr.p2, task.dotSize, task.highlightColor, -1, task.lineType)
            If heartBeat() Then
                strOut1 = Format(mr.correlation1, fmt3)
                strOut2 = Format(mr.correlation2, fmt3)
            End If
            setTrueText(strOut1, mr.p1, 2)
            setTrueText(strOut2, mr.p2, 2)
        Next

        labels(2) = CStr(matches.Count - incorrectCount) + " lines were confirmed with correlations"
        If incorrectCount Then updateLines = True
    End Sub
End Class







Public Class Match_TraceRedC : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32S, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32S, 0)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        desc = "Track each RedCloud cell center to highlight zones of RedCloud cell instability.  Look for clusters of points in dst2."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If heartBeat() Or task.cameraStable = False Then
            dst0.SetTo(0)
            dst1.SetTo(0)
            dst2.SetTo(0)
        End If
        redC.Run(src)

        Static frameList As New List(Of cv.Mat)
        If task.optionsChanged Then frameList.Clear()

        dst0.SetTo(0)
        Dim points As New List(Of cv.Point)

        For Each rc In task.redCells
            dst0.Set(Of Byte)(rc.maxDist.Y, rc.maxDist.X, 1)
        Next
        labels(2) = CStr(task.redCells.Count) + " cells added"

        frameList.Add(dst0.Clone)
        If frameList.Count >= task.historyCount Then
            dst1 = dst1.Subtract(frameList(0))
            frameList.RemoveAt(0)
        End If
        dst1 = dst1.Add(dst0)
        dst1.ConvertTo(dst2, cv.MatType.CV_8U)
        dst2 = dst2.Threshold(0, 255, cv.ThresholdTypes.Binary)
        dst3 = redC.dst2
    End Sub
End Class








Public Class Match_DrawRect : Inherits VB_Algorithm
    Dim match As New Match_Basics
    Public drawRect As cv.Rect
    Public showOutput As Boolean
    Public Sub New()
        drawRect = New cv.Rect(dst2.Width / 2 - 20, dst2.Height / 2 - 20, 40, 40) ' arbitrary template to match
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32F, 0)
        If standalone Then labels(3) = "Probabilities (draw rectangle to test again)"
        labels(2) = "Red dot marks best match for the selected region.  Draw a rectangle anywhere to test again. "
        desc = "Find the requested template in task.drawrect in an image"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static lastImage As cv.Mat = src.Clone
        If task.mouseClickFlag And task.drawRect.Width <> 0 Then
            drawRect = validateRect(task.drawRect)
            match.template = src(drawRect).Clone()
        Else
            If firstPass Then match.template = lastImage(drawRect).Clone()
        End If

        match.Run(src)

        If standalone Or showOutput Then
            dst0 = match.dst0.Normalize(0, 255, cv.NormTypes.MinMax)
            dst3.SetTo(0)
            dst0.CopyTo(dst3(New cv.Rect(drawRect.Width / 2, drawRect.Height / 2, dst0.Width, dst0.Height)))
            dst3.Rectangle(drawRect, cv.Scalar.White, task.lineWidth, task.lineType)
            dst2 = src
        End If

        Dim maxLoc = New cv.Point(match.drawRect.X + drawRect.Width / 2, match.drawRect.Y + drawRect.Height / 2)
        setTrueText("maxLoc = " + CStr(maxLoc.X) + ", " + CStr(maxLoc.Y), New cv.Point(1, 1), 3)
        drawRect = validateRect(New cv.Rect(maxLoc.X - drawRect.Width / 2, maxLoc.Y - drawRect.Height / 2, drawRect.Width, drawRect.Height))

        If standalone Then
            dst2.Circle(maxLoc.X, maxLoc.Y, task.dotSize, cv.Scalar.Red, -1, task.lineType)
            setTrueText(Format(match.correlation, fmt3), maxLoc, 2)
        End If
        lastImage = src
    End Sub
End Class









Public Class Match_FeatureLine : Inherits VB_Algorithm
    Dim finder As New Feature_tCellTracker
    Public Sub New()
        labels = {"", "Output of the Feature_CellFinder", "Best cells highlighted", "Line defined by the best cells"}
        desc = "Find the min and max cells in X and manage a line between them"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        finder.Run(src)
        dst2 = finder.dst2

        dst3 = src
        Dim minX = Integer.MaxValue, maxX = Integer.MinValue
        Dim bestLeft As tCell
        Dim bestRight As tCell
        For Each tc In finder.tcells
            If tc.center.X < minX Then
                minX = tc.center.X
                bestLeft = tc
            End If

            If tc.center.X > maxX Then
                maxX = tc.center.X
                bestRight = tc
            End If
        Next

        If bestLeft.rect <> New cv.Rect And bestRight.rect <> New cv.Rect Then
            src(bestLeft.rect).CopyTo(dst3(bestLeft.rect))
            src(bestRight.rect).CopyTo(dst3(bestRight.rect))
            dst3.Line(bestLeft.center, bestRight.center, task.highlightColor, task.lineWidth, task.lineType)
        End If
    End Sub
End Class









Public Class Match_tCell : Inherits VB_Algorithm
    Public tCells As New List(Of tCell)
    Dim cellSlider As Windows.Forms.TrackBar
    Dim options As New Options_Features
    Public Sub New()
        Dim tc As tCell
        tCells.Add(tc)
        cellSlider = findSlider("MatchTemplate Cell Size")
        desc = "Use MatchTemplate to find the new location of the template and update the tc that was provided."
    End Sub
    Public Function createCell(src As cv.Mat, correlation As Single, pt As cv.Point2f) As tCell
        Dim rSize = cellSlider.Value
        Dim tc As tCell

        tc.rect = validateRect(New cv.Rect(pt.X - rSize, pt.Y - rSize, rSize * 2, rSize * 2))
        tc.correlation = correlation
        tc.depth = task.pcSplit(2)(tc.rect).Mean(task.depthMask(tc.rect))(0) / 1000
        tc.center = pt
        tc.searchRect = validateRect(New cv.Rect(tc.center.X - rSize * 3, tc.center.Y - rSize * 3, rSize * 6, rSize * 6))
        If tc.template Is Nothing Then tc.template = src(tc.rect).Clone
        Return tc
    End Function
    Public Sub RunVB(src as cv.Mat)
        Dim rSize = cellSlider.Value
        If standalone And heartBeat() Then
            options.RunVB()
            tCells.Clear()
            tCells.Add(createCell(src, 0, New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))))
            tCells.Add(createCell(src, 0, New cv.Point(msRNG.Next(0, dst2.Width), msRNG.Next(0, dst2.Height))))
        End If

        For i = 0 To tCells.Count - 1
            Dim tc = tCells(i)
            Dim input = src(tc.searchRect)
            cv.Cv2.MatchTemplate(tc.template, input, dst0, cv.TemplateMatchModes.CCoeffNormed)
            Dim mm = vbMinMax(dst0)
            tc.center = New cv.Point2f(tc.searchRect.X + mm.maxLoc.X + rSize, tc.searchRect.Y + mm.maxLoc.Y + rSize)
            tc.searchRect = validateRect(New cv.Rect(tc.center.X - rSize * 3, tc.center.Y - rSize * 3, rSize * 6, rSize * 6))
            tc.rect = validateRect(New cv.Rect(tc.center.X - rSize, tc.center.Y - rSize, rSize * 2, rSize * 2))
            tc.correlation = mm.maxVal
            tc.depth = task.pcSplit(2)(tc.rect).Mean(task.depthMask(tc.rect))(0) / 1000
            tc.strOut = Format(tc.correlation, fmt2) + vbCrLf + Format(tc.depth, fmt2) + "m"
            tCells(i) = tc
        Next

        If standalone Then
            Static lineDisp As New Line_DisplayInfo
            lineDisp.tcells = tCells
            lineDisp.Run(src)
            dst2 = lineDisp.dst2
        End If
    End Sub
End Class








Public Class Match_LinePairTest : Inherits VB_Algorithm
    Public ptx(2 - 1) As cv.Point2f
    Public target(ptx.Count - 1) As cv.Mat
    Public correlation(ptx.Count - 1)
    Dim options As New Options_Features
    Public Sub New()
        desc = "Use MatchTemplate to find the new location of the template and update the tc that was provided."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static cellSlider = findSlider("MatchTemplate Cell Size")
        Static corrSlider = findSlider("Feature Correlation Threshold")
        Dim minCorrelation = corrSlider.Value / 100
        Dim rSize = cellSlider.Value
        Dim radius = rSize / 2

        Dim rect As cv.Rect

        Options.RunVB()

        If (target(0) IsNot Nothing And correlation(0) < minCorrelation) Then target(0) = Nothing
        If task.mouseClickFlag Then
            ptx(0) = task.clickPoint
            ptx(1) = New cv.Point2f(msRNG.Next(rSize, dst2.Width - 2 * rSize), msRNG.Next(rSize, dst2.Height - 2 * rSize))

            rect = validateRect(New cv.Rect(ptx(0).X - radius, ptx(0).Y - radius, rSize, rSize))
            target(0) = src(rect)

            rect = validateRect(New cv.Rect(ptx(1).X - radius, ptx(1).Y - radius, rSize, rSize))
            target(1) = src(rect)
        End If

        If target(0) Is Nothing Or target(1) Is Nothing Then
            dst3 = src
            setTrueText("Click anywhere in the image to start the algorithm.")
            Exit Sub
        End If

        dst3 = src.Clone
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_32FC1, 0)

        For i = 0 To ptx.Count - 1
            rect = validateRect(New cv.Rect(ptx(i).X - radius, ptx(i).Y - radius, rSize, rSize))
            Dim searchRect = validateRect(New cv.Rect(rect.X - rSize, rect.Y - rSize, rSize * 3, rSize * 3))
            cv.Cv2.MatchTemplate(target(i), src(searchRect), dst0, cv.TemplateMatchModes.CCoeffNormed)
            Dim mmData = vbMinMax(dst0)
            correlation(i) = mmData.maxVal
            If i = 0 Then
                dst0.CopyTo(dst2(New cv.Rect(0, 0, dst0.Width, dst0.Height)))
                dst2 = dst2.Threshold(minCorrelation, 255, cv.ThresholdTypes.Binary)
            End If
            ptx(i) = New cv.Point2f(mmData.maxLoc.X + searchRect.X + radius, mmData.maxLoc.Y + searchRect.Y + radius)
            dst3.Circle(ptx(i), task.dotSize, task.highlightColor, -1, task.lineType)
            dst3.Rectangle(searchRect, cv.Scalar.Yellow, 1)
            rect = validateRect(New cv.Rect(ptx(i).X - radius, ptx(i).Y - radius, rSize, rSize))
            target(i) = task.color(rect)
        Next

        labels(3) = "p1 = " + CStr(ptx(0).X) + "," + CStr(ptx(0).Y) + " p2 = " + CStr(ptx(1).X) + "," + CStr(ptx(1).Y)
        labels(2) = "Correlation = " + Format(correlation(0), fmt3) + " Search result is " + CStr(dst0.Width) + "X" + CStr(dst0.Height)
    End Sub
End Class









'Public Class Match_FeatureTrace : Inherits VB_Algorithm
'    Dim gfKNN As New Match_GoodFeatureKNN
'    Dim match As New Match_Basics
'    Dim redC As New RedColor_Basics
'    Public Sub New()
'        gOptions.useMotion.Checked = False
'        labels = {"", "", "Features identfied in Match_GoodFeaturesKNN - rectangle finds the largest", ""}
'        desc = "Identify and match the paths of good features"
'    End Sub
'    Public Sub RunVB(src as cv.Mat)
'        gfKNN.Run(src)

'        dst0 = gfKNN.dst2.Threshold(0, 255, cv.ThresholdTypes.BinaryInv)
'        dst0.ConvertTo(dst1, cv.MatType.CV_8U)

'        redC.Run(dst1)
'        dst2 = gfKNN.dst2
'        dst2.SetTo(0, dst0)

'        Dim rcBest As rcData
'        For Each rc In task.redCells
'            If rc.rect.Width > dst2.Width / 2 Or rc.rect.Height > dst2.Height / 2 Then Continue For
'            If rc.matchCount > rcBest.matchCount Then rcBest = rc
'        Next

'        If rcBest.rect.Width = 0 Or rcBest.rect.Height = 0 Then Exit Sub

'        match.template = src(rcBest.rect).Clone
'        match.Run(src)
'        Dim r = New cv.Rect(rcBest.rect.X - 20, rcBest.rect.Y - 20, 40, 40)
'        dst2.Rectangle(r, cv.Scalar.White, task.lineWidth)
'        setTrueText("Move camera to see feature trails.", 3)
'    End Sub
'End Class










Public Class Match_GoodFeatureKNN : Inherits VB_Algorithm
    Public knn As New KNN_Lossy
    Public good As New Feature_Basics
    Public Sub New()
        If sliders.Setup(traceName) Then sliders.setupTrackBar("Maximum travel distance per frame", 1, 20, 5)
        dst0 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8UC1, 0)
        labels(3) = "Shake camera to see tracking of the highlighted features"
        desc = "Track the GoodFeatures with KNN"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Static distSlider = findSlider("Maximum travel distance per frame")
        Dim maxDistance = distSlider.Value

        good.Run(src)

        knn.queries = New List(Of cv.Point2f)(good.corners)
        knn.Run(Nothing)

        Static frameList As New List(Of cv.Mat)
        If task.optionsChanged Then
            frameList.Clear()
            dst1.SetTo(0)
        End If

        dst0.SetTo(0)
        For Each mp In knn.matches
            If mp.p1.DistanceTo(mp.p2) <= maxDistance Then dst0.Line(mp.p1, mp.p2, 255, task.lineWidth + 2, cv.LineTypes.Link4)
        Next
        frameList.Add(dst0.Clone)
        If frameList.Count >= task.historyCount Then
            dst1 = dst1.Subtract(frameList(0))
            frameList.RemoveAt(0)
        End If
        dst1 += dst0
        dst2 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)

        dst3 = src
        dst3.SetTo(task.highlightColor, dst2)
    End Sub
End Class







Public Class Match_Point : Inherits VB_Algorithm
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
    Public Sub RunVB(src as cv.Mat)
        If standalone Then
            setTrueText("Set the target mat and the pt then run to track an individual point." + vbCrLf +
                        "After running, the pt is updated with the new location and correlation with the updated correlation." + vbCrLf +
                        "There is no output when run standalone")
            Exit Sub
        End If

        Static cellSlider = findSlider("MatchTemplate Cell Size")
        Dim rSize = cellSlider.Value
        Dim radius = rSize / 2

        Dim rect = validateRect(New cv.Rect(pt.X - radius, pt.Y - radius, rSize, rSize))
        searchRect = validateRect(New cv.Rect(rect.X - rSize, rect.Y - rSize, rSize * 3, rSize * 3))
        cv.Cv2.MatchTemplate(target(rect), src(searchRect), dst0, cv.TemplateMatchModes.CCoeffNormed)
        Dim mmData = vbMinMax(dst0)
        correlation = mmData.maxVal
        pt = New cv.Point2f(mmData.maxLoc.X + searchRect.X + radius, mmData.maxLoc.Y + searchRect.Y + radius)
        src.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        src.Rectangle(searchRect, cv.Scalar.Yellow, 1)
    End Sub
End Class








Public Class Match_Points : Inherits VB_Algorithm
    Public ptx As New List(Of cv.Point2f)
    Public correlation As New List(Of Single)
    Public mPoint As New Match_Point
    Dim options As New Options_Features
    Dim good As New Feature_Basics
    Public Sub New()
        labels(2) = "Rectangle shown is the search rectangle."
        desc = "Track the selected points"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If firstPass Then mPoint.target = src.Clone

        If standalone Then
            good.Run(src)
            ptx = New List(Of cv.Point2f)(good.corners)
            setTrueText("Move camera around to watch the point being tracked", 3)
        End If

        dst2 = src.Clone
        correlation.Clear()
        For i = 0 To ptx.Count - 1
            mPoint.pt = ptx(i)
            mPoint.Run(src)
            correlation.Add(mPoint.correlation)
            ptx(i) = mPoint.pt
            drawFatDot(ptx(i), dst2)
            dst2.Rectangle(mPoint.searchRect, cv.Scalar.Yellow, 1)
        Next
        mPoint.target = src.Clone
    End Sub
End Class