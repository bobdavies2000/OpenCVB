Imports cv = OpenCvSharp
Imports System.Threading
Public Class MatchTemplate_Basics
    Inherits VBparent
    Dim flow As Font_FlowText
    Public searchArea As cv.Mat
    Public template As cv.Mat
    Public matchText As String = ""
    Public correlationMat As New cv.Mat
    Public correlation As Single
    Public matchOption As cv.TemplateMatchModes
    Public Sub New()
        initParent()
        flow = New Font_FlowText()

        If findfrm(caller + " Radio Options") Is Nothing Then
            radio.Setup(caller, 6)
            radio.check(0).Text = "CCoeff"
            radio.check(1).Text = "CCoeffNormed"
            radio.check(2).Text = "CCorr"
            radio.check(3).Text = "CCorrNormed"
            radio.check(4).Text = "SqDiff"
            radio.check(5).Text = "SqDiffNormed"
            radio.check(1).Checked = True
        End If

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Sample Size", 2, 10000, 100)
            sliders.setupTrackBar(1, "Correlation Threshold X100", 1, 100, 90)
        End If
        task.desc = "Find correlation coefficient for 2 random series.  Should be near zero except for small sample size."
		' task.rank = 1
    End Sub
    Public Function checkRadio() As cv.TemplateMatchModes
        matchOption = cv.TemplateMatchModes.CCoeffNormed
        Static frm = findfrm("MatchTemplate_Basics Radio Options")
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
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Static sampleSlider = findSlider("Sample Size")
        If standalone Or task.intermediateReview = caller Then
            searchArea = New cv.Mat(New cv.Size(CInt(sampleSlider.Value), 1), cv.MatType.CV_32FC1)
            template = New cv.Mat(New cv.Size(CInt(sampleSlider.Value), 1), cv.MatType.CV_32FC1)
            cv.Cv2.Randn(searchArea, 100, 25)
            cv.Cv2.Randn(template, 0, 25)
        End If

        matchOption = checkRadio()

        cv.Cv2.MatchTemplate(searchArea, template, correlationMat, matchOption)
        correlation = correlationMat.Get(Of Single)(0, 0)
        label1 = "Correlation = " + Format(correlation, "#,##0.000")
        If standalone Or task.intermediateReview = caller Then
            dst1.SetTo(0)
            label1 = matchText + " for " + CStr(searchArea.Cols) + " samples = " + Format(correlation, "#,##0.00")
            flow.msgs.Add(matchText + " = " + Format(correlation, "#,##0.00"))
            flow.Run(src)
        End If
    End Sub
End Class




Public Class MatchTemplate_RowCorrelation
    Inherits VBparent
    Dim match As MatchTemplate_Basics
    Dim flow As Font_FlowText
    Public Sub New()
        initParent()
        flow = New Font_FlowText()

        match = New MatchTemplate_Basics()

        task.desc = "Find correlation coefficients for 2 random rows in the RGB image to show variability"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim line1 = msRNG.Next(0, src.Height - 1)
        Dim line2 = msRNG.Next(0, src.Height - 1)

        match.searchArea = src.Row(line1)
        match.template = src.Row(line2 + 1)
        match.Run(src)
        Dim correlation = match.correlationMat.Get(Of Single)(0, 0)
        flow.msgs.Add(match.matchText + " between lines " + CStr(line1) + " and line " + CStr(line2) + " = " + Format(correlation, "#,##0.00"))
        flow.Run(src)

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
        label1 = "Min = " + Format(minCorrelation, "#,##0.00") + " max = " + Format(maxCorrelation, "#,##0.0000")
    End Sub
End Class





Public Class MatchTemplate_DrawRect
    Inherits VBparent
    Public saveTemplate As cv.Mat
    Public saveRect As cv.Rect
    Dim match As MatchTemplate_Basics
    Dim addw As AddWeighted_Basics
    Public Sub New()
        initParent()
        If standalone Then task.drawRect = New cv.Rect(100, 100, 50, 50) ' arbitrary template to match

        addw = New AddWeighted_Basics
        match = New MatchTemplate_Basics

        label1 = "Probabilities (draw rectangle to test again)"
        task.desc = "Find the requested template in an image.  Tracker Algorithm"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.drawRect.Width = 0 Or task.drawRect.Height = 0 Then Exit Sub
        If task.drawRect.Width > 0 And task.drawRect.Height > 0 Then
            If task.drawRect.X + task.drawRect.Width >= src.Width Then task.drawRect.Width = src.Width - task.drawRect.X
            If task.drawRect.Y + task.drawRect.Height >= src.Height Then task.drawRect.Height = src.Height - task.drawRect.Y
            saveRect = task.drawRect
            saveTemplate = src(task.drawRect).Clone()
        End If

        match.searchArea = saveTemplate
        match.template = src
        match.Run(src)

        dst1 = New cv.Mat(src.Size, cv.MatType.CV_32F, 0)
        Dim rect = New cv.Rect(task.drawRect.Width / 2, task.drawRect.Height / 2, src.Width - task.drawRect.Width + 1, src.Height - task.drawRect.Height + 1)
        dst1(rect) = match.correlationMat
        dst2 = src

        Dim minVal As Single, maxVal As Single, minLoc As cv.Point, maxLoc As cv.Point
        dst1.MinMaxLoc(minVal, maxVal, minLoc, maxLoc)

        Static thresholdSlider = findSlider("Correlation Threshold X100")
        Dim mask = dst1.Threshold(thresholdSlider.value / 100, 255, cv.ThresholdTypes.Binary)
        mask.ConvertTo(mask, cv.MatType.CV_8U)
        addw.src2 = mask.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        addw.Run(src)
        dst2 = addw.dst1

        dst2.Circle(maxLoc.X, maxLoc.Y, task.dotSize / 2, cv.Scalar.Red, -1, task.lineType)
        label2 = "Red is best match, white has correlation > " + Format(thresholdSlider.value / 100, "#0%")
    End Sub
End Class










Public Class MatchTemplate_BestEntropy_MT
    Inherits VBparent
    Dim entropy As Entropy_Highest
    Dim match As MatchTemplate_DrawRect
    Public Sub New()
        initParent()

        match = New MatchTemplate_DrawRect()

        entropy = New Entropy_Highest()

        label1 = "Probabilities that the template matches image"
        label2 = "Red is the best template to match (highest entropy)"
        task.desc = "Track an object - one with the highest entropy - using OpenCV's matchtemplate.  Tracker Algorithm"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
		If task.intermediateReview = caller Then task.intermediateObject = Me
        If task.frameCount Mod 30 = 0 Then
            entropy.Run(src)
            task.drawRect = entropy.eMaxRect
        End If
        match.Run(src)
        dst1 = match.dst1
        dst2 = match.dst2
    End Sub
End Class












Public Class MatchTemplate_Movement
    Inherits VBparent
    Dim grid As Thread_Grid
    Dim match As MatchTemplate_Basics
    Public mask As cv.Mat
    Public Sub New()
        initParent()
        match = New MatchTemplate_Basics
        grid = New Thread_Grid

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Correlation Threshold X1000", 0, 1000, 970)
            sliders.setupTrackBar(1, "Stdev Threshold", 0, 100, 10)
        End If

        mask = New cv.Mat(dst1.Size, cv.MatType.CV_8U)
        dst2 = mask.Clone
        task.desc = "Assign each segment a correlation coefficient and stdev to the previous frame"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        If task.intermediateReview = caller Then task.intermediateObject = Me
        Dim fsize = task.fontSize / 3

        grid.run(src)
        dst1 = src.Clone
        If dst1.Channels = 3 Then dst1 = dst1.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static stdevSlider = findSlider("Stdev Threshold")
        Dim stdevThreshold = CSng(stdevSlider.Value)

        Static correlationSlider = findSlider("Correlation Threshold X1000")
        Dim CCthreshold = CSng(correlationSlider.Value / correlationSlider.Maximum)

        Static lastFrame As cv.Mat = dst1.Clone()
        Dim saveFrame As cv.Mat = dst1.Clone
        Dim updateCount As Integer
        mask.SetTo(0)

        Dim matchOption = match.checkRadio()

        Parallel.ForEach(grid.roiList,
        Sub(roi)
            Dim mean As Single, stdev As Single
            cv.Cv2.MeanStdDev(dst1(roi), mean, stdev)
            If stdev > stdevThreshold Then
                Dim correlation As New cv.Mat
                cv.Cv2.MatchTemplate(dst1(roi), lastFrame(roi), correlation, matchOption)
                If correlation.Get(Of Single)(0, 0) < CCthreshold Then
                    Interlocked.Increment(updateCount)
                    Dim pt = New cv.Point(roi.X + 2, roi.Y + 10)
                    cv.Cv2.PutText(dst1, Format(correlation.Get(Of Single)(0, 0), "#0.00"), pt, task.font, fsize, cv.Scalar.White, 1, task.lineType)
                Else
                    mask(roi).SetTo(255)
                    dst1(roi).SetTo(0)
                End If
            Else
                Interlocked.Increment(updateCount)
            End If
        End Sub)
        dst1.SetTo(255, grid.gridMask)
        dst2.SetTo(0)
        saveFrame.CopyTo(dst2, mask)
        lastFrame = saveFrame
        Dim corrPercent = Format(correlationSlider.value / 1000, "0.0%") + " correlation"
        label1 = CStr(updateCount) + " of " + CStr(grid.roiList.Count) + " with < " + corrPercent + " or low stdev"
        label2 = CStr(grid.roiList.Count - updateCount) + " segments out of " + CStr(grid.roiList.Count) + " had > " + corrPercent
    End Sub
End Class
