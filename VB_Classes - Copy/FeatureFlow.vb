Imports NAudio
Imports cvb = OpenCvSharp
Public Class FeatureFlow_Basics : Inherits TaskParent
    Public mpList As New List(Of PointPair)
    Public mpCorrelation As New List(Of Single)
    Public Sub New()
        task.gOptions.MaxDepthBar.Value = 20
        If standalone Then task.gOptions.setDisplay1()
        labels(1) = "NOTE: matching right point is always to the left of the left point"
        desc = "Identify which feature in the left image corresponds to the feature in the right image."
    End Sub
    Public Sub buildCorrelations(prevFeatures As List(Of cvb.Point), currFeatures As List(Of cvb.Point))
        Dim correlationMin = task.feat.options.correlationMin

        Dim correlationmat As New cvb.Mat
        mpList.Clear()
        mpCorrelation.Clear()
        Dim pad = task.feat.options.templatePad, size = task.feat.options.templateSize
        For Each p1 In prevFeatures
            Dim rect = ValidateRect(New cvb.Rect(p1.X - pad, p1.Y - pad, size, size))
            Dim correlations As New List(Of Single)
            For Each p2 In currFeatures
                Dim r = ValidateRect(New cvb.Rect(p2.X - pad, p2.Y - pad, Math.Min(rect.Width, size), Math.Min(size, rect.Height)))
                cvb.Cv2.MatchTemplate(dst2(rect), dst3(r), correlationmat, cvb.TemplateMatchModes.CCoeffNormed)
                correlations.Add(correlationmat.Get(Of Single)(0, 0))
            Next
            Dim maxCorrelation = correlations.Max
            If maxCorrelation >= correlationMin Then
                Dim index = correlations.IndexOf(maxCorrelation)
                mpList.Add(New PointPair(p1, currFeatures(index)))
                mpCorrelation.Add(maxCorrelation)
            End If
        Next
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        labels = task.feat.labels

        dst3 = If(task.FirstPass, src.Clone, dst2.Clone)
        Static prevFeatures As New List(Of cvb.Point)(task.featurePoints)
        buildCorrelations(prevFeatures, task.featurePoints)

        SetTrueText("Click near any feature to find the corresponding pair of features.", 1)
        dst2 = src.Clone
        For Each pt In task.featurePoints
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next
        prevFeatures = New List(Of cvb.Point)(task.featurePoints)
    End Sub
End Class






'https://www.learnopencvb.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
Public Class FeatureFlow_Dense : Inherits TaskParent
    Dim options As New Options_OpticalFlow
    Public Sub New()
        desc = "Use dense optical flow algorithm  "
    End Sub

    Public Sub RunAlg(src As cvb.Mat)
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        options.RunOpt()
        Static lastGray As cvb.Mat = src.Clone
        Dim hsv = opticalFlow_Dense(lastGray, src, options.pyrScale, options.levels, options.winSize, options.iterations, options.polyN,
                                    options.polySigma, options.OpticalFlowFlags)

        dst2 = hsv.CvtColor(cvb.ColorConversionCodes.HSV2RGB)
        dst2 = dst2.ConvertScaleAbs(options.outputScaling)
        dst3 = dst2.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        lastGray = src.Clone()
    End Sub
End Class







' https://www.learnopencvb.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
Public Class FeatureFlow_LucasKanade : Inherits TaskParent
    Public features As New List(Of cvb.Point2f)
    Public lastFeatures As New List(Of cvb.Point2f)
    Dim options As New Options_OpticalFlowSparse
    Public Sub New()
        desc = "Show the optical flow of a sparse matrix."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        dst2 = src.Clone()
        dst3 = src.Clone()

        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)
        Static lastGray As cvb.Mat = src.Clone
        features = task.features
        Dim features1 = cvb.Mat.FromPixelData(features.Count, 1, cvb.MatType.CV_32FC2, features.ToArray)
        Dim features2 = New cvb.Mat
        Dim status As New cvb.Mat, err As New cvb.Mat, winSize As New cvb.Size(3, 3)
        cvb.Cv2.CalcOpticalFlowPyrLK(src, lastGray, features1, features2, status, err, winSize, 3, term, options.OpticalFlowFlag)
        features = New List(Of cvb.Point2f)
        lastFeatures.Clear()
        For i = 0 To status.Rows - 1
            If status.Get(Of Byte)(i, 0) Then
                Dim pt1 = features1.Get(Of cvb.Point2f)(i, 0)
                Dim pt2 = features2.Get(Of cvb.Point2f)(i, 0)
                Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If length < 30 Then
                    features.Add(pt1)
                    lastFeatures.Add(pt2)
                    dst2.Line(pt1, pt2, task.HighlightColor, task.lineWidth + task.lineWidth, task.lineType)
                    DrawCircle(dst3, pt1, task.DotSize + 3, white)
                    DrawCircle(dst3, pt2, task.DotSize + 1, cvb.Scalar.Red)
                End If
            End If
        Next
        labels(2) = "Matched " + CStr(features.Count) + " points "

        If task.heartBeat Then lastGray = src.Clone()
        lastGray = src.Clone()
    End Sub
End Class






Public Class FeatureFlow_LeftRight1 : Inherits TaskParent
    Dim pyrLeft As New FeatureFlow_LucasKanade
    Dim pyrRight As New FeatureFlow_LucasKanade
    Dim ptLeft As New List(Of cvb.Point)
    Dim ptRight As New List(Of cvb.Point)
    Public ptlist As New List(Of cvb.Point)
    Public Sub New()
        If standalone Then task.gOptions.setDisplay1()
        desc = "Find features using optical flow in both the left and right images."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        pyrLeft.Run(task.leftView)
        pyrRight.Run(task.rightView)

        Dim leftY As New List(Of Integer)
        ptLeft.Clear()
        dst2 = task.leftView.Clone
        For i = 0 To pyrLeft.features.Count - 1
            Dim pt = pyrLeft.features(i)
            ptLeft.Add(New cvb.Point(pt.X, pt.Y))
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            leftY.Add(pt.Y)

            pt = pyrLeft.lastFeatures(i)
            ptLeft.Add(New cvb.Point(pt.X, pt.Y))
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            leftY.Add(pt.Y)
        Next

        Dim rightY As New List(Of Integer)
        ptRight.Clear()
        dst3 = task.rightView.Clone
        For i = 0 To pyrRight.features.Count - 1
            Dim pt = pyrRight.features(i)
            ptRight.Add(New cvb.Point(pt.X, pt.Y))
            DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
            rightY.Add(pt.Y)

            pt = pyrRight.lastFeatures(i)
            ptRight.Add(New cvb.Point(pt.X, pt.Y))
            DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
            rightY.Add(pt.Y)
        Next

        Dim mpList As New List(Of PointPair)
        ptlist.Clear()
        For i = 0 To leftY.Count - 1
            Dim index = rightY.IndexOf(leftY(i))
            If index >= 0 Then mpList.Add(New PointPair(ptLeft(i), ptRight(index)))
        Next

        If task.heartBeat Then
            labels(2) = CStr(ptLeft.Count) + " features found in the left image, " + CStr(ptRight.Count) + " features in the right and " +
                        CStr(ptlist.Count) + " features are matched."
        End If
    End Sub
End Class






Public Class FeatureFlow_LeftRightHist : Inherits TaskParent
    Dim pyrLeft As New FeatureFlow_LucasKanade
    Dim pyrRight As New FeatureFlow_LucasKanade
    Public leftFeatures As New List(Of cvb.Point)
    Public rightFeatures As New List(Of cvb.Point)
    Public Sub New()
        desc = "Keep only the features that have been around for the specified number of frames."
    End Sub
    Public Function displayFeatures(dst As cvb.Mat, features As List(Of cvb.Point)) As cvb.Mat
        For Each pt In features
            DrawCircle(dst, pt, task.DotSize, task.HighlightColor)
        Next
        Return dst
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        pyrLeft.Run(task.leftView)
        Dim tmpLeft As New List(Of cvb.Point)
        For i = 0 To pyrLeft.features.Count - 1
            Dim pt = New cvb.Point(pyrLeft.features(i).X, pyrLeft.features(i).Y)
            tmpLeft.Add(New cvb.Point(pt.X, pt.Y))
            pt = New cvb.Point(pyrLeft.lastFeatures(i).X, pyrLeft.lastFeatures(i).Y)
            tmpLeft.Add(New cvb.Point(pt.X, pt.Y))
        Next

        pyrRight.Run(task.rightView)
        Dim tmpRight As New List(Of cvb.Point)
        For i = 0 To pyrRight.features.Count - 1
            Dim pt = New cvb.Point(pyrRight.features(i).X, pyrRight.features(i).Y)
            tmpRight.Add(New cvb.Point(pt.X, pt.Y))
            pt = New cvb.Point(pyrRight.lastFeatures(i).X, pyrRight.lastFeatures(i).Y)
            tmpRight.Add(New cvb.Point(pt.X, pt.Y))
        Next

        Static leftHist As New List(Of List(Of cvb.Point))({tmpLeft})
        Static rightHist As New List(Of List(Of cvb.Point))({tmpRight})

        If task.optionsChanged Then
            leftHist = New List(Of List(Of cvb.Point))({tmpLeft})
            rightHist = New List(Of List(Of cvb.Point))({tmpRight})
        End If

        leftFeatures.Clear()
        For Each pt In tmpLeft
            Dim count As Integer = 0
            For Each hist In leftHist
                If hist.Contains(pt) Then count += 1 Else Exit For
            Next
            If count = leftHist.Count Then leftFeatures.Add(pt)
        Next

        rightFeatures.Clear()
        For Each pt In tmpRight
            Dim count As Integer = 0
            For Each hist In rightHist
                If hist.Contains(pt) Then count += 1 Else Exit For
            Next
            If count = rightHist.Count Then rightFeatures.Add(pt)
        Next

        Dim minPoints = 10 ' just a guess - trying to keep things current.
        If leftFeatures.Count < minPoints Then
            leftFeatures = tmpLeft
            leftHist = New List(Of List(Of cvb.Point))({tmpLeft})
        End If
        If rightFeatures.Count < minPoints Then
            rightFeatures = tmpRight
            rightHist = New List(Of List(Of cvb.Point))({tmpRight})
        End If

        dst2 = displayFeatures(task.leftView.Clone, leftFeatures)
        dst3 = displayFeatures(task.rightView.Clone, rightFeatures)

        leftHist.Add(tmpLeft)
        rightHist.Add(tmpRight)
        Dim threshold = Math.Min(task.gOptions.FrameHistory.Value, leftHist.Count)

        If leftHist.Count >= task.gOptions.FrameHistory.Value Then leftHist.RemoveAt(0)
        If rightHist.Count >= task.gOptions.FrameHistory.Value Then rightHist.RemoveAt(0)

        If task.heartBeat Then
            labels(2) = CStr(leftFeatures.Count) + " detected in the left image that have matches in " + CStr(threshold) + " previous left images"
            labels(3) = CStr(rightFeatures.Count) + " detected in the right image that have matches in " + CStr(threshold) + " previous right images"
        End If
    End Sub
End Class






Public Class FeatureFlow_LeftRight : Inherits TaskParent
    Dim flowHist As New FeatureFlow_LeftRightHist
    Public leftFeatures As New List(Of List(Of cvb.Point))
    Public rightFeatures As New List(Of List(Of cvb.Point))
    Public Sub New()
        desc = "Match features in the left and right images"
    End Sub
    Public Function displayFeatures(dst As cvb.Mat, features As List(Of List(Of cvb.Point))) As cvb.Mat
        For Each ptlist In features
            For Each pt In ptlist
                DrawCircle(dst, pt, task.DotSize, task.HighlightColor)
            Next
        Next
        Return dst
    End Function
    Public Sub RunAlg(src As cvb.Mat)
        flowHist.Run(src)

        Dim tmpLeft As New SortedList(Of Integer, List(Of cvb.Point))
        Dim ptlist As List(Of cvb.Point)
        For Each pt In flowHist.leftFeatures
            If tmpLeft.Keys.Contains(pt.Y) Then
                Dim index = tmpLeft.Keys.IndexOf(pt.Y)
                ptlist = tmpLeft.ElementAt(index).Value
                ptlist.Add(pt)
                tmpLeft.RemoveAt(index)
            Else
                ptlist = New List(Of cvb.Point)({pt})
            End If
            tmpLeft.Add(pt.Y, ptlist)
        Next

        Dim tmpRight As New SortedList(Of Integer, List(Of cvb.Point))
        For Each pt In flowHist.rightFeatures
            If tmpRight.Keys.Contains(pt.Y) Then
                Dim index = tmpRight.Keys.IndexOf(pt.Y)
                ptlist = tmpRight.ElementAt(index).Value
                ptlist.Add(pt)
                tmpRight.RemoveAt(index)
            Else
                ptlist = New List(Of cvb.Point)({pt})
            End If
            tmpRight.Add(pt.Y, ptlist)
        Next

        leftFeatures.Clear()
        rightFeatures.Clear()
        For Each ele In tmpLeft
            Dim index = tmpRight.Keys.IndexOf(ele.Key)
            If index >= 0 Then
                leftFeatures.Add(ele.Value)
                rightFeatures.Add(tmpRight.ElementAt(index).Value)
            End If
        Next

        dst2 = displayFeatures(task.leftView.Clone, leftFeatures)
        dst3 = displayFeatures(task.rightView.Clone, rightFeatures)

        If task.heartBeat Then
            labels(2) = CStr(leftFeatures.Count) + " detected in the left image that match one or more Y-coordinates found in the right image"
            labels(3) = CStr(rightFeatures.Count) + " detected in the right image that match one or more Y-coordinates found in the left image"
        End If
    End Sub
End Class
