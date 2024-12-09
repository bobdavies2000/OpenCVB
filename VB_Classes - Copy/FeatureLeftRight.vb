Imports cvb = OpenCvSharp
Public Class FeatureLeftRight_Basics : Inherits TaskParent
    Dim prep As New FeatureLeftRight_LeftRightPrep
    Public mpList As New List(Of PointPair)
    Public mpCorrelation As New List(Of Single)
    Public selectedPoint As cvb.Point, mpIndex
    Dim ClickPoint As cvb.Point, picTag As Integer
    Dim options As New Options_Features
    Dim knn As New KNN_Basics
    Public Sub New()
        labels(1) = "NOTE: matching right point is always to the left of the left point"
        If standalone Then task.gOptions.setDisplay1()
        FindSlider("Feature Correlation Threshold").Value = 75
        FindSlider("Min Distance to next").Value = 1
        task.gOptions.MaxDepthBar.Value = 20
        labels(3) = "Click near any feature to get more details on the matched pair of points."
        desc = "Match the left and right features and allow the user to select a point to get more details."
    End Sub
    Public Sub setClickPoint(pt As cvb.Point2f, _pictag As Integer)
        ClickPoint = New cvb.Point(pt.X, pt.Y)
        picTag = _pictag
        task.drawRect = New cvb.Rect(ClickPoint.X - options.templatePad, ClickPoint.Y - options.templatePad, options.templateSize, options.templateSize)
        task.drawRectUpdated = True
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = task.leftView.Clone
        dst3 = task.rightView.Clone
        prep.Run(src)

        Dim prepList As New List(Of PointPair)
        For Each p1 In prep.leftFeatures
            For Each p2 In prep.rightFeatures
                If p1.Y = p2.Y Then prepList.Add(New PointPair(p1, p2))
            Next
        Next

        Dim correlationmat As New cvb.Mat
        mpList.Clear()
        mpCorrelation.Clear()
        For i = 0 To prepList.Count - 1
            Dim mpBase = prepList(i)
            Dim correlations As New List(Of Single)
            Dim tmpList As New List(Of PointPair)

            For j = i To prepList.Count - 1
                Dim mp = prepList(j)
                If mp.p1.Y <> mpBase.p1.Y Then
                    i = j
                    Exit For
                End If
                Dim r1 = ValidateRect(New cvb.Rect(mp.p1.X - options.templatePad, mp.p1.Y - options.templatePad,
                                                      options.templateSize, options.templateSize))
                Dim r2 = ValidateRect(New cvb.Rect(mp.p2.X - options.templatePad, mp.p2.Y - options.templatePad,
                                                  options.templateSize, options.templateSize))
                cvb.Cv2.MatchTemplate(task.leftView(r1), task.rightView(r2), correlationmat, cvb.TemplateMatchModes.CCoeffNormed)
                correlations.Add(correlationmat.Get(Of Single)(0, 0))
                tmpList.Add(mp)
            Next

            Dim maxCorrelation = correlations.Max
            If maxCorrelation >= options.correlationMin Then
                mpList.Add(tmpList(correlations.IndexOf(maxCorrelation)))
                mpCorrelation.Add(maxCorrelation)
            End If
        Next

        For Each mp In mpList
            DrawCircle(dst2,mp.p1, task.DotSize, task.HighlightColor)
            DrawCircle(dst3,mp.p2, task.DotSize, task.HighlightColor)
        Next

        If task.mouseClickFlag Then setClickPoint(task.ClickPoint, task.mousePicTag)

        SetTrueText("Click near any feature to find the corresponding pair of features." + vbCrLf +
                    "The correlation values in the lower left for the correlation of the left to the right views." + vbCrLf +
                    "The dst2 shows features for the left view, dst3 shows features for the right view.", 1)
        If ClickPoint = newPoint And mpList.Count > 0 Then setClickPoint(mpList(0).p1, 2)
        If mpList.Count > 0 Then
            knn.queries.Clear()
            knn.queries.Add(task.ClickPoint)

            Dim mp As PointPair
            knn.trainInput.Clear()
            For Each mp In mpList
                Dim pt = If(picTag = 2, mp.p1, mp.p2)
                knn.trainInput.Add(New cvb.Point2f(pt.X, pt.Y))
            Next
            knn.Run(Nothing)

            dst1.SetTo(0)
            Dim mpIndex = knn.result(0, 0)
            mp = mpList(mpIndex)

            DrawCircle(dst2,mp.p1, task.DotSize + 4, cvb.Scalar.Red)
            DrawCircle(dst3,mp.p2, task.DotSize + 4, cvb.Scalar.Red)

            Dim dspDistance = task.pcSplit(2).Get(Of Single)(mp.p1.Y, mp.p1.X)

            Dim offset = mp.p1.X - mp.p2.X
            strOut = Format(mpCorrelation(mpIndex), fmt3) + vbCrLf + Format(dspDistance, fmt3) + "m (from camera)" + vbCrLf +
                            CStr(offset) + " Pixel difference"

            For i = 0 To mpList.Count - 1
                Dim pt = mpList(i).p1
                SetTrueText(Format(mpCorrelation(i), "0%"), pt)
            Next

            If task.heartBeat Then dst1.SetTo(0)
            DrawCircle(dst1,mp.p1, task.DotSize, task.HighlightColor)
            DrawCircle(dst1,mp.p2, task.DotSize, task.HighlightColor)

            selectedPoint = New cvb.Point(mp.p1.X, mpList(mpIndex).p1.Y + 10)
            SetTrueText(strOut, selectedPoint, 1)
            If task.heartBeat Then
                labels(2) = CStr(mpList.Count) + " features matched and confirmed with left/right image correlation coefficients"
            End If
        End If

        labels(2) = CStr(mpList.Count) + " features were matched using correlation coefficients in the left and right images. White box is cell around click point."
    End Sub
End Class







Public Class FeatureLeftRight_LeftRightPrep : Inherits TaskParent
    Dim lFeat As New Feature_Stable
    Dim rFeat As New Feature_Stable
    Public leftFeatures As New List(Of cvb.Point)
    Public rightFeatures As New List(Of cvb.Point)
    Dim saveLFeatures As New List(Of cvb.Point2f)
    Dim saveRFeatures As New List(Of cvb.Point2f)
    Public Sub New()
        desc = "Prepare features for the left and right views"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.features = New List(Of cvb.Point2f)(saveLFeatures)
        lFeat.Run(task.leftView)
        dst2 = lFeat.dst2
        labels(2) = lFeat.labels(2)
        leftFeatures = New List(Of cvb.Point)(task.featurePoints)
        saveLFeatures = New List(Of cvb.Point2f)(task.features)

        task.features = New List(Of cvb.Point2f)(saveRFeatures)
        rFeat.Run(task.rightView)
        dst3 = rFeat.dst2
        labels(3) = rFeat.labels(2)
        rightFeatures = New List(Of cvb.Point)(task.featurePoints)
        saveRFeatures = New List(Of cvb.Point2f)(task.features)
    End Sub
End Class






Public Class FeatureLeftRight_Grid : Inherits TaskParent
    Dim match As New FeatureLeftRight_Basics
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        FindRadio("GoodFeatures (ShiTomasi) grid").Checked = True
        desc = "Run FeatureLeftRight_Basics but with 'GoodFeatures grid' instead of 'GoodFeatures full image'"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        match.Run(src)
        If match.mpList.Count = 0 Then Exit Sub
        dst1 = match.dst1.Clone
        dst2 = match.dst2.Clone
        dst3 = match.dst3.Clone
        If task.FirstPass Then match.setClickPoint(match.mpList(0).p1, 2)
        SetTrueText(match.strOut, match.selectedPoint, 1)
        If task.heartBeat Then labels = match.labels
    End Sub
End Class





Public Class FeatureLeftRight_Input : Inherits TaskParent
    Dim ptLeft As New List(Of cvb.Point)
    Dim ptRight As New List(Of cvb.Point)
    Public mpList As New List(Of PointPair)
    Public mpCorrelation As New List(Of Single)
    Public selectedPoint As cvb.Point, mpIndex
    Dim ClickPoint As cvb.Point, picTag As Integer
    Dim options As New Options_Features
    Dim knn As New KNN_Basics
    Public Sub New()
        labels(1) = "NOTE: matching right point is always to the left of the left point"
        If standalone Then task.gOptions.setDisplay1()
        FindSlider("Feature Correlation Threshold").Value = 75
        FindSlider("Min Distance to next").Value = 1
        task.gOptions.MaxDepthBar.Value = 20 ' up to 20 meters...
        labels(3) = "Click near any feature to get more details on the matched pair of points."
        desc = "Match the left and right features and allow the user to select a point to get more details."
    End Sub
    Public Sub setClickPoint(pt As cvb.Point, _pictag As Integer)
        ClickPoint = pt
        picTag = _pictag
        task.drawRect = New cvb.Rect(ClickPoint.X - options.templatePad, ClickPoint.Y - options.templatePad, options.templateSize, options.templateSize)
        task.drawRectUpdated = True
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        If ptLeft.Count = 0 Or ptRight.Count = 0 Then
            SetTrueText("Caller provides the ptLeft/ptRight points to use.", 1)
            Exit Sub
        End If

        options.RunOpt()

        Dim prepList As New List(Of PointPair)
        For Each p1 In ptLeft
            For Each p2 In ptRight
                If p1.Y = p2.Y Then prepList.Add(New PointPair(p1, p2))
            Next
        Next

        Dim correlationmat As New cvb.Mat
        mpList.Clear()
        mpCorrelation.Clear()
        For i = 0 To prepList.Count - 1
            Dim mpBase = prepList(i)
            Dim correlations As New List(Of Single)
            Dim tmpList As New List(Of PointPair)

            For j = i To prepList.Count - 1
                Dim mp = prepList(j)
                If mp.p1.Y <> mpBase.p1.Y Then
                    i = j
                    Exit For
                End If
                Dim r1 = ValidateRect(New cvb.Rect(mp.p1.X - options.templatePad, mp.p1.Y - options.templatePad,
                                                      options.templateSize, options.templateSize))
                Dim r2 = ValidateRect(New cvb.Rect(mp.p2.X - options.templatePad, mp.p2.Y - options.templatePad,
                                                  options.templateSize, options.templateSize))
                cvb.Cv2.MatchTemplate(task.leftView(r1), task.rightView(r2), correlationmat, cvb.TemplateMatchModes.CCoeffNormed)
                correlations.Add(correlationmat.Get(Of Single)(0, 0))
                tmpList.Add(mp)
            Next

            Dim maxCorrelation = correlations.Max
            If maxCorrelation >= options.correlationMin Then
                mpList.Add(tmpList(correlations.IndexOf(maxCorrelation)))
                mpCorrelation.Add(maxCorrelation)
            End If
        Next

        For Each mp In mpList
            DrawCircle(dst2,mp.p1, task.DotSize, task.HighlightColor)
            DrawCircle(dst3,mp.p2, task.DotSize, task.HighlightColor)
        Next

        If task.mouseClickFlag Then setClickPoint(task.ClickPoint, task.mousePicTag)

        SetTrueText("Click near any feature to find the corresponding pair of features." + vbCrLf +
                    "The correlation values in the lower left for the correlation of the left to the right views." + vbCrLf +
                    "The dst2 shows features for the left view, dst3 shows features for the right view.", 1)
        If ClickPoint = newPoint And mpList.Count > 0 Then setClickPoint(mpList(0).p1, 2)
        If mpList.Count > 0 Then
            knn.queries.Clear()
            knn.queries.Add(task.ClickPoint)

            Dim mp As PointPair
            knn.trainInput.Clear()
            For Each mp In mpList
                Dim pt = If(picTag = 2, mp.p1, mp.p2)
                knn.trainInput.Add(New cvb.Point2f(pt.X, pt.Y))
            Next
            knn.Run(Nothing)

            dst1.SetTo(0)
            Dim mpIndex = knn.result(0, 0)
            mp = mpList(mpIndex)

            DrawCircle(dst2,mp.p1, task.DotSize + 4, cvb.Scalar.Red)
            DrawCircle(dst3,mp.p2, task.DotSize + 4, cvb.Scalar.Red)

            Dim dspDistance = task.pcSplit(2).Get(Of Single)(mp.p1.Y, mp.p1.X)

            Dim offset = mp.p1.X - mp.p2.X
            strOut = Format(mpCorrelation(mpIndex), fmt3) + vbCrLf + Format(dspDistance, fmt3) + "m (from camera)" + vbCrLf +
                            CStr(offset) + " Pixel difference"

            For i = 0 To mpList.Count - 1
                Dim pt = mpList(i).p1
                SetTrueText(Format(mpCorrelation(i), "0%"), pt)
            Next

            If task.heartBeat Then dst1.SetTo(0)
            DrawCircle(dst1,mp.p1, task.DotSize, task.HighlightColor)
            DrawCircle(dst1,mp.p2, task.DotSize, task.HighlightColor)

            selectedPoint = New cvb.Point(mp.p1.X, mpList(mpIndex).p1.Y + 10)
            SetTrueText(strOut, selectedPoint, 1)
            If task.heartBeat Then
                labels(2) = CStr(mpList.Count) + " features matched and confirmed with left/right image correlation coefficients"
            End If
        End If

        labels(2) = CStr(mpList.Count) + " features were matched using correlation coefficients in the left and right images. White box is cell around click point."
    End Sub
End Class