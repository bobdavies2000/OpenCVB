Imports cv = OpenCvSharp
Imports System.Threading
Public Class MatchTemplate_Basics : Inherits VBparent
    Dim flow As New Font_FlowText
    Public searchArea As cv.Mat
    Public template As cv.Mat
    Public matchText As String = ""
    Public correlationMat As New cv.Mat
    Public correlation As Single
    Public matchOption As cv.TemplateMatchModes
    Public Sub New()
        If radio.Setup(caller, 6) Then
            radio.check(0).Text = "CCoeff"
            radio.check(1).Text = "CCoeffNormed"
            radio.check(2).Text = "CCorr"
            radio.check(3).Text = "CCorrNormed"
            radio.check(4).Text = "SqDiff"
            radio.check(5).Text = "SqDiffNormed"
            radio.check(1).Checked = True
        End If

        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Sample Size", 2, 10000, 100)
            sliders.setupTrackBar(1, "Correlation Threshold X100", 1, 100, 90)
        End If
        task.desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
    End Sub
    Public Function checkRadio() As cv.TemplateMatchModes
        matchOption = cv.TemplateMatchModes.CCoeffNormed
        Static frm = findfrm(caller + " Radio Options")
        For i = 0 To frm.check.length - 1
            If frm.check(i).Checked Then
                matchOption = Choose(i + 1, cv.TemplateMatchModes.CCoeff, cv.TemplateMatchModes.CCoeffNormed, cv.TemplateMatchModes.CCorr,
                                            cv.TemplateMatchModes.CCorrNormed, cv.TemplateMatchModes.SqDiff, cv.TemplateMatchModes.SqDiffNormed)
                matchText = Choose(i + 1, "CCoeff", "CCoeffNormed", "CCorr", "CCorrNormed", "SqDiff", "SqDiffNormed")
                Exit For
            End If
        Next
        Return matchOption
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static sampleSlider = findSlider("Sample Size")
        If standalone Or task.intermediateActive Then
            searchArea = New cv.Mat(New cv.Size(CInt(sampleSlider.Value), 1), cv.MatType.CV_32FC1)
            template = New cv.Mat(New cv.Size(CInt(sampleSlider.Value), 1), cv.MatType.CV_32FC1)
            cv.Cv2.Randn(searchArea, 100, 25)
            cv.Cv2.Randn(template, 0, 25)
        End If

        matchOption = checkRadio()

        cv.Cv2.MatchTemplate(searchArea, template, correlationMat, matchOption)
        correlation = correlationMat.Get(Of Single)(0, 0)
        labels(2) = "Correlation = " + Format(correlation, "#,##0.000")
        If standalone Or task.intermediateActive Then
            dst2.SetTo(0)
            labels(2) = matchText + " for " + CStr(searchArea.Cols) + " samples = " + Format(correlation, "#,##0.00")
            flow.msgs.Add(matchText + " = " + Format(correlation, "#,##0.00"))
            flow.RunClass(Nothing)
        End If
    End Sub
End Class




Public Class MatchTemplate_RowCorrelation : Inherits VBparent
    Dim match As New MatchTemplate_Basics
    Dim flow As New Font_FlowText
    Public Sub New()
        task.desc = "Find correlation coefficients for 2 random rows in the RGB image to show variability"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim line1 = msRNG.Next(0, src.Height - 1)
        Dim line2 = msRNG.Next(0, src.Height - 1)

        match.searchArea = src.Row(line1)
        match.template = src.Row(line2 + 1)
        match.RunClass(src)
        Dim correlation = match.correlationMat.Get(Of Single)(0, 0)
        flow.msgs.Add(match.matchText + " between lines " + CStr(line1) + " and line " + CStr(line2) + " = " + Format(correlation, "#,##0.00"))
        flow.RunClass(Nothing)

        Static minCorrelation As Single
        Static maxCorrelation As Single

        Static saveCorrType = match.matchOption
        If task.frameCount = 0 Or saveCorrType <> match.matchOption Then
            minCorrelation = Single.PositiveInfinity
            maxCorrelation = Single.NegativeInfinity
            saveCorrType = match.matchOption
        End If

        If correlation < minCorrelation Then minCorrelation = correlation
        If correlation > maxCorrelation Then maxCorrelation = correlation
        labels(2) = "Min = " + Format(minCorrelation, "#,##0.00") + " max = " + Format(maxCorrelation, "#,##0.0000")
    End Sub
End Class





Public Class MatchTemplate_DrawRect : Inherits VBparent
    Public saveTemplate As cv.Mat
    Public saveRect As cv.Rect
    Dim match As New MatchTemplate_Basics
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        If standalone Then task.drawRect = New cv.Rect(100, 100, 50, 50) ' arbitrary template to match
        labels(2) = "Probabilities (draw rectangle to test again)"
        task.desc = "Find the requested template in an image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static thresholdSlider = findSlider("Correlation Threshold X100")
        If task.drawRect.Width = 0 Or task.drawRect.Height = 0 Then Exit Sub
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
            If task.drawRect.X + task.drawRect.Width >= src.Width Then task.drawRect.Width = src.Width - task.drawRect.X
            If task.drawRect.Y + task.drawRect.Height >= src.Height Then task.drawRect.Height = src.Height - task.drawRect.Y
            saveRect = task.drawRect
            saveTemplate = src(task.drawRect).Clone()
        End If

        match.searchArea = saveTemplate
        match.template = src
        match.RunClass(src)

        dst2 = New cv.Mat(src.Size, cv.MatType.CV_32F, 0)
        Dim rect = New cv.Rect(task.drawRect.Width / 2, task.drawRect.Height / 2, src.Width - task.drawRect.Width + 1, src.Height - task.drawRect.Height + 1)

        If match.correlationMat.Rows = rect.Height And match.correlationMat.Cols = rect.Width Then dst2(rect) = match.correlationMat
        dst3 = src

        Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
        dst2.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        Dim mask = dst2.Threshold(thresholdSlider.value / 100, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        addw.src2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.RunClass(src)
        dst3 = addw.dst2

        dst3.Circle(maxLoc.X, maxLoc.Y, task.dotSize, cv.Scalar.Red, -1, task.lineType)
        labels(3) = "Red is best match, white has correlation > " + Format(thresholdSlider.value / 100, "#0%")
    End Sub
End Class










Public Class MatchTemplate_BestEntropy_MT : Inherits VBparent
    Dim entropy As New Entropy_Highest
    Dim match As New MatchTemplate_DrawRect
    Public Sub New()
        labels(2) = "Probabilities that the template matches image"
        labels(3) = "Red is the best template to match (highest entropy)"
        task.desc = "Track an object - one with the highest entropy - using OpenCV's matchtemplate."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If task.frameCount Mod 30 = 0 Then
            entropy.RunClass(src)
            task.drawRect = entropy.eMaxRect
        End If
        match.RunClass(src)
        dst2 = match.dst2
        dst3 = match.dst3
    End Sub
End Class












Public Class MatchTemplate_Movement : Inherits VBparent
    Dim grid As New Thread_Grid
    Dim match As New MatchTemplate_Basics
    Public mask As cv.Mat
    Public Sub New()
        If sliders.Setup(caller) Then
            sliders.setupTrackBar(0, "Correlation Threshold X1000", 0, 1000, 970)
            sliders.setupTrackBar(1, "Stdev Threshold", 0, 100, 10)
        End If

        mask = New cv.Mat(dst2.Size, cv.MatType.CV_8U)
        dst3 = mask.Clone
        task.desc = "Assign each segment a correlation coefficient and stdev to the previous frame"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static stdevSlider = findSlider("Stdev Threshold")
        Static correlationSlider = findSlider("Correlation Threshold X1000")
        Dim stdevThreshold = CSng(stdevSlider.Value)
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)

        Dim fsize = task.fontSize / 3

        grid.RunClass(Nothing)
        dst2 = src.Clone
        If dst2.Channels = 3 Then dst2 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static lastFrame As cv.Mat = dst2.Clone()
        Dim saveFrame As cv.Mat = dst2.Clone
        Dim updateCount As Integer
        mask.SetTo(0)

        Dim matchOption = match.checkRadio()

        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(dst2(roi), mean, stdev)
            If stdev > stdevThreshold Then
                Dim correlation As New cv.Mat
                cv.Cv2.MatchTemplate(dst2(roi), lastFrame(roi), correlation, matchOption)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                    Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                    cv.Cv2.PutText(dst2, Format(correlation.Get(Of Single)(0, 0), "#0.00"), pt, task.font, fsize, cv.Scalar.White, task.lineWidth, task.lineType)
                Else
                    mask(roi).SetTo(255)
                    dst2(roi).SetTo(0)
                End If
            Else
                Interlocked.Increment(updateCount)
            End If
        End Sub)
        dst2.SetTo(255, grid.gridMask)
        dst3.SetTo(0)
        saveFrame.CopyTo(dst3, mask)
        lastFrame = saveFrame
        Dim corrPercent = Format(correlationSlider.value / 1000, "0.0%") + " correlation"
        labels(2) = CStr(updateCount) + " of " + CStr(grid.roiList.Count) + " with < " + corrPercent + " or low stdev"
        labels(3) = CStr(grid.roiList.Count - updateCount) + " segments out of " + CStr(grid.roiList.Count) + " had > " + corrPercent
    End Sub
End Class
