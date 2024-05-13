Imports cv = OpenCvSharp
Public Class FeatureMatch_Basics : Inherits VB_Algorithm
    Public feat As New FeatureMatch_LeftRight
    Public mpList As New List(Of pointPair)
    Public vecList As New List(Of cv.Point3f)
    Public corrList As New List(Of Single)
    Dim addw As New AddWeighted_Basics
    Dim dst2X As cv.Mat
    Dim leftView As cv.Mat, rightView As cv.Mat
    Public Sub New()
        findSlider("MatchTemplate Cell Size").Value = 6
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        dst2X = New cv.Mat(dst2.Height, dst2.Width * 2, cv.MatType.CV_8U)
        labels = {"", "The AddWeighted_Basics output showing both the left and right images with corresponding points",
                  "Draw a rectangle anywhere to isolate specific good features", ""}
        desc = "Pair left/right features using correlation coefficients"
    End Sub
    Private Sub highlight(mp As pointPair)
        dst2X.Line(mp.p1, New cv.Point2f(mp.p2.X + dst2.Width, mp.p2.Y), cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
    Public Function showMatches(mplist As List(Of pointPair), corrlist As List(Of Single)) As Integer
        Dim r1 As New cv.Rect(0, 0, dst2.Width, dst2.Height)
        Dim r2 As New cv.Rect(dst2.Width, 0, dst2.Width, dst2.Height)
        leftView.CopyTo(dst2X(r1))
        rightView.CopyTo(dst2X(r2))
        Dim r = task.drawRect
        Dim matchCount As Integer
        For i = 0 To mplist.Count - 1
            Dim mp = mplist(i)
            If r.Width > 0 Then
                If mp.p1.X >= r.X And mp.p1.X <= r.X + r.Width And mp.p1.Y >= r.Y And mp.p1.Y <= r.Y + r.Height Then
                    highlight(mp)
                    matchCount += 1
                    setTrueText(Format(corrlist(i), fmt2), mp.p2, 3)
                End If
            Else
                highlight(mp)
                matchCount += 1
            End If
        Next
        addw.src2 = task.color.Clone
        addw.Run(rightView.CvtColor(cv.ColorConversionCodes.GRAY2BGR))
        dst1 = addw.dst2
        dst2 = dst2X(r1)
        dst3 = dst2X(r2)

        For Each mp In mplist
            dst1.Line(mp.p1, mp.p2, cv.Scalar.White, task.lineWidth)
        Next

        Return matchCount
    End Function
    Public Sub RunVB(src As cv.Mat)
        If task.leftView.Channels = 3 Then
            leftView = task.leftView.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            rightView = task.rightView.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Else
            leftView = task.leftView.Clone
            rightView = task.rightView.Clone
        End If

        feat.Run(empty)
        If feat.leftCorners.Count = 0 Then
            dst3.SetTo(0)
            setTrueText("No corners were found in the image.", 3)
            Exit Sub ' nothing found?  Pretty extreme but can happen in darkness.
        End If

        Dim correlationmat As New cv.Mat, rSize = feat.feat.options.fOptions.boxSize, roi = feat.feat.options.roi
        Dim rightIndex As Integer = 0, rectL = roi, rectR = roi, lastKey = feat.leftCorners.ElementAt(0).Key
        Dim correlations As New List(Of Single)
        mpList.Clear()
        corrList.Clear()
        vecList.Clear()
        Dim minCorr = feat.feat.options.fOptions.correlationThreshold
        For Each entry In feat.leftCorners
            Dim p1 = entry.Value
            If entry.Key <> lastKey Then rightIndex += correlations.Count
            rectL.X = p1.X - rSize
            rectL.Y = p1.Y - rSize
            correlations.Clear()
            For i = rightIndex To feat.rightCorners.Count - 1
                Dim rightEntry = feat.rightCorners.ElementAt(i)
                If rightEntry.Key <> p1.Y Then Exit For
                Dim p2 = rightEntry.Value
                rectR.X = p2.X - rSize
                rectR.Y = p2.Y - rSize
                cv.Cv2.MatchTemplate(leftView(rectL), rightView(rectR), correlationmat, feat.feat.options.matchOption)
                correlations.Add(correlationmat.Get(Of Single)(0, 0))
            Next

            Dim maxCorrelation = correlations.Max
            If maxCorrelation > minCorr Then
                Dim maxIndex = correlations.IndexOf(maxCorrelation)
                Dim p2 = feat.rightCorners.ElementAt(rightIndex + maxIndex).Value
                Dim vec3d = task.pointCloud.Get(Of cv.Point3f)(p1.Y, p1.X)
                If vec3d.Z > 0 Then
                    vecList.Add(vec3d)
                    mpList.Add(New pointPair(p1, p2))
                    corrList.Add(maxCorrelation)
                    dst1.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
                End If
            End If
            lastKey = entry.Key
        Next

        Dim matchcount = showMatches(mpList, corrList)
        If task.heartBeat Then
            labels(3) = CStr(matchcount) + " points were matched with a minimum " + Format(minCorr, fmt1) +
                        " correlation"
        End If
    End Sub
End Class






Public Class FeatureMatch_LeftRight : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public leftCorners As New SortedList(Of Integer, cv.Point2f)(New compareAllowIdenticalInteger)
    Public rightCorners As New SortedList(Of Integer, cv.Point2f)(New compareAllowIdenticalInteger)
    Public Sub New()
        findSlider("Min Distance to next").Value = 1
        labels = {"", "", "Features detected in the left image", "Features deteced in the right image"}
        desc = "Detect good features in the left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(task.rightView)
        Dim tmpRight As New List(Of cv.Point2f)
        Dim ptRight As New List(Of Integer)
        For Each pt In task.features
            tmpRight.Add(pt)
            ptRight.Add(CInt(pt.Y))
        Next
        dst3 = feat.dst2

        feat.Run(task.leftView)

        Dim tmpLeft As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
        For Each pt In task.features
            tmpLeft.Add(pt.Y, pt)
        Next
        dst2 = feat.dst2

        leftCorners.Clear()
        rightCorners.Clear()
        Dim rowList As New List(Of Integer)
        Dim rSize = feat.options.fOptions.boxSize
        For Each entry In tmpLeft
            Dim row = entry.Key
            Dim index = ptRight.IndexOf(row)
            If index < 0 Then Continue For
            Dim p1 = entry.Value
            If row >= rSize And row <= dst2.Height - rSize And p1.X >= rSize And p1.X <= dst2.Width - rSize Then
                Dim p2 = tmpRight(index)
                If p2.X >= rSize And p2.X <= dst2.Width - rSize Then
                    leftCorners.Add(row, p1)
                    rightCorners.Add(p2.Y, p2)
                    tmpRight.RemoveAt(index)
                    ptRight.RemoveAt(index)
                    If rowList.Contains(row) = False Then rowList.Add(row)
                End If
            End If
        Next
        If task.heartBeat Then
            labels(2) = "There were " + CStr(rowList.Count) + " rows with features in both images."
            labels(3) = "Left image has " + CStr(leftCorners.Count) + " good features and the right camera has " +
                                       CStr(rightCorners.Count) + " good features"
        End If
    End Sub
End Class







'Public Class FeatureMatch_History : Inherits VB_Algorithm
'    Public feat As New FeatureMatch_Basics
'    Public mpList As New List(Of pointPair)
'    Public corrList As New List(Of Single)
'    Dim matchCount As Integer
'    Public Sub New()
'        If standaloneTest() Then gOptions.displayDst1.Checked = True
'        labels(1) = "The AddWeighted output of the left and right images connecting the corresponding points."
'        desc = "Pair left/right features using correlation coefficients"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static thresholdSlider = findSlider("Feature Correlation Threshold")
'        Dim minCorr = thresholdSlider.value / 100

'        Static allLists As New List(Of List(Of pointPair))
'        Static allCorrs As New List(Of List(Of Single))
'        If task.optionsChanged Then
'            allLists.Clear()
'            allCorrs.Clear()
'        End If

'        feat.Run(empty)

'        allLists.Add(New List(Of pointPair)(feat.mpList))
'        allCorrs.Add(New List(Of Single)(feat.corrList))

'        If allLists.Count > task.frameHistoryCount Then
'            allLists.RemoveAt(0)
'            allCorrs.RemoveAt(0)
'        End If

'        mpList.Clear()
'        corrList.Clear()

'        For i = 0 To allLists.Count - 1
'            For Each mp In allLists(i)
'                mpList.Add(mp)
'                Dim index = allLists(i).IndexOf(mp)
'                corrList.Add(allCorrs(i)(index))
'            Next
'        Next

'        feat.showMatches(mpList, corrList)
'        dst1 = feat.dst1
'        dst2 = feat.dst2
'        dst3 = feat.dst3

'        trueData = New List(Of trueText)(feat.trueData)
'        If task.heartBeat Then
'            labels(3) = CStr(mpList.Count) + " points were matched with a minimum " + Format(minCorr, fmt1) + " correlation"
'        End If
'    End Sub
'End Class








'Public Class FeatureMatch_Validate2 : Inherits VB_Algorithm
'    Public feat As New Feature_Basics
'    Dim ptList As New List(Of cv.Point)
'    Public Sub New()
'        If standalone Then gOptions.displayDst1.Checked = True
'        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32S, 0)
'        desc = "Isolate only the features present on the current frame and the previous"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        feat.Run(src)

'        If task.optionsChanged Then dst1.SetTo(0)

'        If task.heartBeat Then
'            dst0 = dst1.InRange(1, 255)
'            dst1.SetTo(0, Not dst0)
'        End If

'        dst2 = src
'        For Each pt In task.features
'            Dim count = dst1.Get(Of Byte)(CInt(pt.Y), CInt(pt.X))
'            If count < 0 Then count = 0
'            dst1.Set(Of Integer)(CInt(pt.Y), CInt(pt.X), count + 2)
'        Next

'        dst1 -= 1
'        dst1.ConvertTo(dst0, cv.MatType.CV_8U)
'        dst3 = dst0.Threshold(0, 255, cv.ThresholdTypes.Binary)

'        Dim tmp = dst3.FindNonZero()
'        ptList.Clear()
'        For i = 0 To tmp.Rows - 1
'            Dim pt = tmp.Get(Of cv.Point)(i, 0)
'            ptList.Add(pt)
'            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'        Next

'        labels(2) = CStr(ptList.Count) + " points were the same as in previous frames."
'    End Sub
'End Class







'Public Class FeatureMatch_Validate3 : Inherits VB_Algorithm
'    Public feat As New Feature_Basics
'    Dim ptList As New List(Of cv.Point)
'    Public Sub New()
'        If standalone Then gOptions.displayDst1.Checked = True
'        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32S, 0)
'        desc = "Isolate only the features present on the current frame and the previous"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static ptLists As New List(Of List(Of cv.Point))

'        feat.Run(src)
'        Dim fList As New List(Of cv.Point)
'        For Each pt In task.features
'            fList.Add(New cv.Point(pt.X, pt.Y))
'        Next
'        ptLists.Add(fList)

'        dst2 = src
'        ptList.Clear()
'        Dim ptCount As New List(Of Integer)
'        For Each ptSet In ptLists
'            For Each pt In ptSet
'                Dim index = ptList.IndexOf(pt)
'                If index >= 0 Then
'                    ptCount(index) += 1
'                Else
'                    ptList.Add(pt)
'                    ptCount.Add(1)
'                End If
'            Next
'        Next

'        Dim minCount = gOptions.FrameHistory.Value
'        For i = 0 To ptList.Count - 1
'            Dim pt = ptList(i)
'            If ptCount(i) > minCount Then dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'        Next

'        If ptLists.Count > gOptions.FrameHistory.Value Then ptLists.RemoveAt(0)
'        labels(2) = CStr(ptList.Count) + " points were the same as in previous frames."
'    End Sub
'End Class






'Public Class FeatureMatch_Validate4 : Inherits VB_Algorithm
'    Public feat As New Feature_Basics
'    Dim ptList As New List(Of cv.Point)
'    Public Sub New()
'        gOptions.DebugSlider.Value = 95
'        If standalone Then gOptions.displayDst1.Checked = True
'        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32S, 0)
'        desc = "Isolate only the features present on the current frame and the previous"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static cellSizeSlider = findSlider("MatchTemplate Cell Size")
'        Static correlationSlider = findSlider("Feature Correlation Threshold")
'        Dim minCorrelation = correlationSlider.value / 100
'        Dim boxSize = cellSizeSlider.value
'        Static lastImage As cv.Mat = src.Clone
'        feat.Run(src)

'        Dim newList As New List(Of cv.Point)
'        For Each pt In task.features
'            Dim iPt = New cv.Point(pt.X, pt.Y)
'            newList.Add(iPt)

'            Dim index = ptList.IndexOf(iPt)
'            If index >= 0 Then ptList.RemoveAt(index)
'        Next

'        dst2 = src
'        For Each pt In ptList
'            If pt.X >= dst2.Width - boxSize Then Continue For
'            If pt.Y >= dst2.Height - boxSize Then Continue For
'            Dim r = New cv.Rect(pt.X, pt.Y, boxSize, boxSize)
'            cv.Cv2.MatchTemplate(src(r), lastImage(r), dst0, cv.TemplateMatchModes.CCoeffNormed)
'            Dim correlation = vbMinMax(dst0).maxVal
'            If correlation >= minCorrelation Then newList.Add(pt)
'        Next

'        ptList = New List(Of cv.Point)(newList)

'        For Each pt In ptList
'            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'        Next
'        lastImage = src.Clone
'        labels(2) = CStr(ptList.Count) + " points were the same as in previous frames."
'    End Sub
'End Class







'Public Class FeatureMatch_Validate5 : Inherits VB_Algorithm
'    Public feat As New Feature_Basics
'    Dim ptList As New List(Of cv.Point)
'    Dim templates As New List(Of cv.Mat)
'    Dim halfSize As Integer
'    Dim boxSize As Integer
'    Public Sub New()
'        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32F, 0)
'        desc = "Isolate only the features present on the current frame and the previous"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static correlationSlider = findSlider("Feature Correlation Threshold")
'        Static cellSizeSlider = findSlider("MatchTemplate Cell Size")
'        Dim minCorrelation = correlationSlider.value / 100
'        boxSize = cellSizeSlider.value
'        halfSize = cellSizeSlider.value / 2

'        If ptList.Count < 20 Or gOptions.DebugCheckBox.Checked Then
'            gOptions.DebugCheckBox.Checked = False
'            feat.Run(src)

'            ptList.Clear()
'            For Each pt In task.features
'                Dim iPt = New cv.Point(pt.X, pt.Y)
'                If vbEdgeTest(iPt, boxSize) Then
'                    Dim r = New cv.Rect(iPt.X - halfSize, iPt.Y - halfSize, boxSize, boxSize)
'                    ptList.Add(iPt)
'                    templates.Add(src(r).Clone)
'                End If
'            Next
'        End If
'        Dim ptSort As New SortedList(Of Single, cv.Point)(New compareAllowIdenticalSingleInverted)
'        For i = 0 To ptList.Count - 1
'            Dim pt = ptList(i)
'            Dim template = templates(i)
'            Dim rect = validateRect(New cv.Rect(pt.X - boxSize * 2, pt.Y - boxSize * 2, boxSize * 4, boxSize * 4))
'            cv.Cv2.MatchTemplate(template, src(rect), dst0, cv.TemplateMatchModes.CCoeffNormed)
'            Dim mm = vbMinMax(dst0)
'            ptSort.Add(mm.maxVal, New cv.Point(rect.X + mm.maxLoc.X + halfSize, rect.Y + mm.maxLoc.Y + halfSize))
'        Next

'        dst2 = src
'        ptList.Clear()
'        templates.Clear()
'        For i = 0 To ptSort.Count - 1
'            If ptSort.ElementAt(i).Key < minCorrelation Then Exit For
'            Dim pt = ptSort.ElementAt(i).Value
'            If vbEdgeTest(pt, boxSize) Then
'                Dim r = New cv.Rect(pt.X - halfSize, pt.Y - halfSize, boxSize, boxSize)
'                ptList.Add(pt)
'                templates.Add(src(r).Clone)
'                dst2.Rectangle(r, task.highlightColor, task.lineWidth, task.lineType)
'            End If
'        Next
'        For Each pt In ptList
'            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'        Next
'        labels(2) = CStr(task.features.Count) + " points from Feature_Basics were whittled to " + CStr(ptList.Count)
'    End Sub
'End Class






'Public Class FeatureMatch_Validate6 : Inherits VB_Algorithm
'    Public feat As New Feature_Basics
'    Dim ptList As New List(Of cv.Point)
'    Dim templates As New List(Of cv.Rect)
'    Dim halfSize As Integer
'    Dim boxSize As Integer
'    Public Sub New()
'        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32F, 0)
'        desc = "Isolate only the features present on the current frame and the previous - Not working."
'    End Sub
'    Private Function edgeTest(pt As cv.Point) As Boolean
'        If pt.X < halfSize Then Return False
'        If pt.Y < halfSize Then Return False
'        If pt.X >= dst2.Width - halfSize Then Return False
'        If pt.Y >= dst2.Height - halfSize Then Return False
'        Return True
'    End Function
'    Public Sub RunVB(src As cv.Mat)
'        Static correlationSlider = findSlider("Feature Correlation Threshold")
'        Static cellSizeSlider = findSlider("MatchTemplate Cell Size")
'        Dim minCorrelation = correlationSlider.value / 100
'        boxSize = cellSizeSlider.value
'        halfSize = cellSizeSlider.value / 2

'        If ptList.Count < 20 Or gOptions.DebugCheckBox.Checked Then
'            gOptions.DebugCheckBox.Checked = False
'            feat.Run(src)

'            ptList.Clear()
'            For Each pt In task.features
'                Dim iPt = New cv.Point(pt.X, pt.Y)
'                If edgeTest(iPt) Then
'                    ptList.Add(iPt)
'                    templates.Add(New cv.Rect(iPt.X - halfSize, iPt.Y - halfSize, boxSize, boxSize))
'                End If
'            Next
'        End If

'        Dim ptSort As New SortedList(Of Single, cv.Point)(New compareAllowIdenticalSingleInverted)
'        For i = 0 To ptList.Count - 1
'            Dim pt = ptList(i)
'            Dim r = templates(i)
'            Dim rect = validateRect(New cv.Rect(pt.X - boxSize, pt.Y - boxSize, boxSize * 2, boxSize * 2))
'            cv.Cv2.MatchTemplate(dst3(r), src(rect), dst0, cv.TemplateMatchModes.CCoeffNormed)
'            Dim mm = vbMinMax(dst0)
'            ptSort.Add(mm.maxVal, New cv.Point(rect.X + mm.maxLoc.X + halfSize, rect.Y + mm.maxLoc.Y + halfSize))
'        Next

'        dst2 = src
'        ptList.Clear()
'        templates.Clear()
'        For i = 0 To ptSort.Count - 1
'            If ptSort.ElementAt(i).Key < minCorrelation Then Exit For
'            Dim pt = ptSort.ElementAt(i).Value
'            If edgeTest(pt) Then
'                ptList.Add(pt)
'                templates.Add(New cv.Rect(pt.X - halfSize, pt.Y - halfSize, boxSize, boxSize))
'            End If
'        Next
'        For Each pt In ptList
'            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'        Next
'        dst3 = src.Clone
'        labels(2) = CStr(task.features.Count) + " points from Feature_Basics were whittled to " + CStr(ptList.Count)
'    End Sub
'End Class





'Public Class FeatureMatch_Entropy1 : Inherits VB_Algorithm
'    Dim match As New Match_Basics
'    Dim roiList As New List(Of cv.Rect)
'    Dim entropy As New Entropy_SubDivisions
'    Public roiNewList As New List(Of cv.Rect)
'    Dim roiCorr As New List(Of Single)
'    Public Sub New()
'        desc = "Isolate only the features present on the current frame and the previous - Not working."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static correlationSlider = findSlider("Feature Correlation Threshold")
'        Dim minCorrelation = correlationSlider.value / 100

'        Dim roiList = New List(Of cv.Rect)(entropy.roiList)
'        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        Static lastImage = src.Clone
'        dst2 = src
'        If task.heartBeat Then
'            If entropy.roiList.Count = 0 Then entropy.Run(src)
'            For Each roi In entropy.roiList
'                dst2.Rectangle(roi, cv.Scalar.White, task.lineWidth, task.lineType)
'            Next

'            roiNewList.Clear()
'            roiCorr.Clear()
'            For Each roi In roiList
'                match.template = lastImage(roi)
'                match.Run(src)
'                Dim pt = New cv.Point(match.mmData.maxLoc.X, match.mmData.maxLoc.Y)
'                Dim r = New cv.Rect(pt.X, pt.Y, roi.Width, roi.Height)
'                roiNewList.Add(r)
'                roiCorr.Add(match.correlation)
'                setTrueText(Format(match.correlation, fmt3), New cv.Point(r.X, r.Y), 3)
'            Next
'            lastImage = src.Clone
'        End If
'        dst3.SetTo(0)
'        For i = 0 To roiNewList.Count - 1
'            'If roiCorr(i) > minCorrelation Then
'            dst2.Rectangle(roiList(i), cv.Scalar.White, task.lineWidth + 2, task.lineType)
'            dst2.Rectangle(roiNewList(i), task.highlightColor, task.lineWidth, task.lineType)
'            setTrueText(Format(roiCorr(i), fmt3), New cv.Point(roiNewList(i).X, roiNewList(i).Y), 3)
'            'End If
'        Next
'    End Sub
'End Class





'Public Class FeatureMatch_Entropy2 : Inherits VB_Algorithm
'    Dim match As New Match_Basics
'    Dim entropy As New Entropy_SubDivisions
'    Public Sub New()
'        gOptions.DebugCheckBox.Checked = True
'        desc = "Isolate only the features present on the current frame and the previous - Not working."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        Static lastImage = src.Clone
'        dst2 = src.Clone
'        If gOptions.DebugCheckBox.Checked Then
'            gOptions.DebugCheckBox.Checked = False
'            entropy.Run(src)
'            lastImage = src.Clone
'        End If

'        For Each roi In entropy.roiList
'            match.template = lastImage(roi)
'            match.Run(src)
'            dst2.Circle(match.matchCenter, task.dotSize, cv.Scalar.White, -1, task.lineType)
'            setTrueText(Format(match.correlation, fmt3), match.matchCenter, 3)
'        Next
'    End Sub
'End Class







'Public Class FeatureMatch_Validate : Inherits VB_Algorithm
'    Public feat As New Feature_Basics
'    Dim match As New Match_Basics
'    Dim ptList As New List(Of cv.Point)
'    Dim rectList As New List(Of cv.Rect)
'    Public Sub New()
'        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_32F, 0)
'        desc = "Isolate only the features present on the current frame and the previous."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Dim boxSize = match.options.boxSize, halfSize = match.options.halfSize
'        If ptList.Count < 20 Or gOptions.DebugCheckBox.Checked Then
'            gOptions.DebugCheckBox.Checked = False
'            dst3 = src.Clone
'            feat.Run(src)

'            ptList.Clear()
'            For Each pt In task.features
'                Dim iPt = New cv.Point(pt.X, pt.Y)
'                If vbEdgeTest(iPt, boxSize) Then
'                    ptList.Add(iPt)
'                    rectList.Add(New cv.Rect(iPt.X - halfSize, iPt.Y - halfSize, boxSize, boxSize))
'                End If
'            Next
'        End If

'        Dim ptSort As New SortedList(Of Single, cv.Point)(New compareAllowIdenticalSingleInverted)
'        For i = 0 To ptList.Count - 1
'            Dim pt = ptList(i)
'            Dim r = New cv.Rect(pt.X - boxSize, pt.Y - boxSize, boxSize * 2, boxSize * 2)
'            match.template = dst3(r)
'            match.Run(src)
'            ptSort.Add(match.mmData.maxVal, match.matchCenter)
'        Next

'        dst2 = src
'        ptList.Clear()
'        rectList.Clear()
'        Dim minCorrelation = match.options.correlationThreshold
'        For i = 0 To ptSort.Count - 1
'            If ptSort.ElementAt(i).Key < minCorrelation Then Exit For
'            Dim pt = ptSort.ElementAt(i).Value
'            If vbEdgeTest(pt, boxSize) Then
'                ptList.Add(pt)
'                rectList.Add(New cv.Rect(pt.X - halfSize, pt.Y - halfSize, boxSize, boxSize))
'            End If
'        Next
'        For Each pt In ptList
'            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'        Next
'        labels(2) = CStr(task.features.Count) + " points from Feature_Basics were whittled to " + CStr(ptList.Count)
'    End Sub
'End Class





'Public Class FeatureMatch_GridPoints : Inherits VB_Algorithm
'    Dim features As New Feature_Basics
'    Dim match As New Match_Basics
'    Public Sub New()
'        If standalone Then gOptions.displayDst1.Checked = True
'        desc = "Isolate only the features present on the current frame and the previous - Not working."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        Static correlationSlider = findSlider("Feature Correlation Threshold")
'        Static cellSizeSlider = findSlider("MatchTemplate Cell Size")
'        Dim minCorrelation = correlationSlider.value / 100
'        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        dst2 = src
'        Static correlations(task.gridList.Count - 1) As Single
'        Static templates(task.gridList.Count - 1) As cv.Mat
'        Static matchCenters(task.gridList.Count - 1) As cv.Point2f
'        If task.optionsChanged Then
'            ReDim matchCenters(task.gridList.Count - 1)
'            ReDim correlations(task.gridList.Count - 1)
'            ReDim templates(task.gridList.Count - 1)
'            Exit Sub
'        End If
'        Dim gridSize = gOptions.GridSize.Value
'        For i = 0 To task.gridList.Count - 1
'            Dim roi = task.gridList(i)
'            If roi.X < gridSize Then Continue For
'            If roi.X > dst2.Width - gridSize * 2 Then Continue For
'            If roi.Y < gridSize Then Continue For
'            If roi.Y > dst2.Height - gridSize Then Continue For
'            If correlations(i) < minCorrelation Then
'                features.Run(src(roi))
'                If task.features.Count = 0 Then
'                    matchCenters(i) = New cv.Point2f
'                    templates(i) = Nothing
'                Else
'                    matchCenters(i) = New cv.Point2f(roi.X + task.features(0).X, roi.Y + task.features(0).Y)
'                    templates(i) = src(roi)
'                End If
'            End If
'        Next

'        Dim boxSize = cellSizeSlider.value Or 1
'        Dim halfSize As Integer = boxSize / 2
'        For i = 0 To matchCenters.Count - 1
'            Dim pt = matchCenters(i)
'            If pt = New cv.Point2f Then Continue For

'            match.template = templates(i)
'            match.Run(src)
'            matchCenters(i) = match.matchCenter
'            correlations(i) = match.correlation
'            setTrueText(Format(match.correlation, fmt3), match.matchCenter, 1)
'        Next

'        For i = 0 To matchCenters.Count - 1
'            Dim pt = matchCenters(i)
'            If pt = New cv.Point2f Then Continue For
'            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
'        Next
'    End Sub
'End Class





'Public Class FeatureMatch_Entropy : Inherits VB_Algorithm
'    Dim match As New Match_Basics
'    Dim entropy As New Entropy_SubDivisions
'    Public Sub New()
'        gOptions.DebugCheckBox.Checked = True
'        desc = "Isolate only the features present on the current frame and the previous - Not working."
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
'        Static lastImage = src.Clone
'        dst2 = src.Clone
'        If gOptions.DebugCheckBox.Checked Then
'            gOptions.DebugCheckBox.Checked = False
'            entropy.Run(src)
'            lastImage = src.Clone
'        End If

'        For Each roi In entropy.roiList
'            match.template = lastImage(roi)
'            match.Run(src)
'            dst2.Circle(match.matchCenter, task.dotSize, cv.Scalar.White, -1, task.lineType)
'            setTrueText(Format(match.correlation, fmt3), match.matchCenter, 3)
'        Next
'    End Sub
'End Class





'Public Class FeatureMatch_Validate1 : Inherits VB_Algorithm
'    Public feat As New Feature_Basics
'    Public ptList As New List(Of cv.Point)
'    Dim ptCount As New List(Of Integer)
'    Public Sub New()
'        desc = "Isolate only the features present on the current frame and the previous"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        feat.Run(src)

'        Dim fList As New List(Of cv.Point)
'        For Each pt In task.features
'            fList.Add(New cv.Point(pt.X, pt.Y))
'        Next

'        Dim lastCounts As New List(Of Integer)
'        For Each count In ptCount
'            lastCounts.Add(count - 1)
'        Next

'        dst2 = src
'        For i = 0 To fList.Count - 1
'            Dim pt = fList(i)
'            Dim index = ptList.IndexOf(pt)
'            If index >= 0 Then
'                ptCount(index) += 1
'                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'            Else
'                ptList.Add(pt)
'                ptCount.Add(1)
'            End If
'        Next

'        For i = lastCounts.Count - 1 To 0 Step -1
'            If ptCount(i) = lastCounts(i) Then
'                ptList.RemoveAt(i)
'                ptCount.RemoveAt(i)
'            End If
'        Next

'        labels(2) = CStr(ptList.Count) + " points were the same as in previous frames."
'    End Sub
'End Class











'Public Class FeatureMatch_Validate8 : Inherits VB_Algorithm
'    Public feat As New Feature_Basics
'    Public ptList As New List(Of cv.Point2f)
'    Dim ptCount As New List(Of Integer)
'    Public Sub New()
'        desc = "Isolate only the features present on the current frame and the previous"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        feat.Run(src)

'        Dim ptNewList As New List(Of cv.Point2f)
'        Dim fList As New List(Of cv.Point)
'        For Each pt In task.features
'            fList.Add(New cv.Point(pt.X, pt.Y))
'        Next

'        dst2 = src
'        Dim removeList As New List(Of Integer)
'        For Each pt In ptList
'            Dim ptInt = New cv.Point(CInt(pt.X), CInt(pt.Y))
'            If fList.Contains(ptInt) Then
'                dst2.Circle(pt, task.dotSize, cv.Scalar.Red, -1, task.lineType)
'                ptNewList.Add(pt)
'                removeList.Add(ptList.IndexOf(pt))
'            Else
'                dst2.Circle(pt, task.dotSize, cv.Scalar.Yellow, -1, task.lineType)
'            End If
'        Next

'        For i = removeList.Count - 1 To 0 Step -1
'            ptList.RemoveAt(removeList(i))
'        Next

'        For Each pt In task.features
'            ptNewList.Add(pt)
'        Next

'        ptList = New List(Of cv.Point2f)(ptNewList)

'        'For i = 0 To fList.Count - 1
'        '    Dim pt = fList(i)
'        '    Dim index = ptList.IndexOf(pt)
'        '    If index >= 0 Then
'        '        ptCount(index) += 1
'        '        dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'        '    Else
'        '        ptList.Add(pt)
'        '        ptCount.Add(1)
'        '    End If
'        'Next

'        'For i = lastCounts.Count - 1 To 0 Step -1
'        '    If ptCount(i) = lastCounts(i) Then
'        '        ptList.RemoveAt(i)
'        '        ptCount.RemoveAt(i)
'        '    End If
'        'Next

'        labels(2) = CStr(ptList.Count) + " points were the same as in previous frames."
'    End Sub
'End Class

Public Class FeatureMatch_LeftRightNew : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim lrHist As New FeatureMatch_LeftRightHist
    Public leftFeatures As New List(Of List(Of cv.Point))
    Public rightFeatures As New List(Of List(Of cv.Point))
    Public Sub New()
        desc = "Match features in the left and right images"
    End Sub
    Public Function displayFeatures(dst As cv.Mat, features As List(Of List(Of cv.Point))) As cv.Mat
        For Each ptlist In features
            For Each pt In ptlist
                dst.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            Next
        Next
        Return dst
    End Function
    Public Sub RunVB(src As cv.Mat)
        lrHist.Run(src)

        Dim tmpLeft As New SortedList(Of Integer, List(Of cv.Point))
        Dim ptlist As List(Of cv.Point)
        For Each pt In lrHist.leftPoints
            If tmpLeft.Keys.Contains(pt.Y) Then
                Dim index = tmpLeft.Keys.IndexOf(pt.Y)
                ptlist = tmpLeft.ElementAt(index).Value
                ptlist.Add(pt)
                tmpLeft.RemoveAt(index)
            Else
                ptlist = New List(Of cv.Point)
                ptlist.Add(pt)
            End If
            tmpLeft.Add(pt.Y, ptlist)
        Next

        Dim tmpRight As New SortedList(Of Integer, List(Of cv.Point))
        For Each pt In lrHist.rightPoints
            If tmpRight.Keys.Contains(pt.Y) Then
                Dim index = tmpRight.Keys.IndexOf(pt.Y)
                ptlist = tmpRight.ElementAt(index).Value
                ptlist.Add(pt)
                tmpRight.RemoveAt(index)
            Else
                ptlist = New List(Of cv.Point)
                ptlist.Add(pt)
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
            labels(2) = CStr(leftFeatures.Count) + " detected in the left image that have matches in the right image"
            labels(3) = CStr(rightFeatures.Count) + " detected in the right image that have matches in the left image"
        End If
    End Sub
End Class








Public Class FeatureMatch_LeftRightHist : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Public leftPoints As New List(Of cv.Point)
    Public rightPoints As New List(Of cv.Point)
    Public Sub New()
        findSlider("Min Distance to next").Value = 1
        gOptions.FrameHistory.Value = 10
        desc = "Keep only the features that have been around for the specified number of frames."
    End Sub
    Public Function displayFeatures(dst As cv.Mat, features As List(Of cv.Point)) As cv.Mat
        For Each pt In features
            dst.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        Return dst
    End Function
    Public Sub RunVB(src As cv.Mat)
        Dim minPoints = 10

        feat.Run(task.leftView)
        Dim tmpLeft As New List(Of cv.Point)
        For Each pt In task.features
            tmpLeft.Add(New cv.Point(pt.X, pt.Y))
        Next

        feat.Run(task.rightView)
        Dim tmpRight As New List(Of cv.Point)
        For Each pt In task.features
            tmpRight.Add(New cv.Point(pt.X, pt.Y))
        Next

        Static leftHist As New List(Of List(Of cv.Point))({tmpLeft})
        Static rightHist As New List(Of List(Of cv.Point))({tmpRight})

        If task.optionsChanged Then
            leftHist = New List(Of List(Of cv.Point))({tmpLeft})
            rightHist = New List(Of List(Of cv.Point))({tmpRight})
        End If

        Dim saveLeft As New List(Of cv.Point)
        For Each pt In tmpLeft
            Dim count As Integer = 0
            For Each hist In leftHist
                If hist.Contains(pt) Then count += 1 Else Exit For
            Next
            If count = leftHist.Count Then saveLeft.Add(pt)
        Next
        If saveLeft.Count < minPoints Then leftPoints.Clear() Else leftPoints = saveLeft

        Dim saveRight As New List(Of cv.Point)
        For Each pt In tmpRight
            Dim count As Integer = 0
            For Each hist In rightHist
                If hist.Contains(pt) Then count += 1 Else Exit For
            Next
            If count = rightHist.Count Then saveRight.Add(pt)
        Next
        If saveRight.Count < minPoints Then rightPoints.Clear() Else rightPoints = saveRight

        If leftPoints.Count < minPoints Then
            leftPoints = tmpLeft
            leftHist = New List(Of List(Of cv.Point))({tmpLeft})
        End If
        If rightPoints.Count < 10 Then
            rightPoints = tmpRight
            rightHist = New List(Of List(Of cv.Point))({tmpRight})
        End If

        dst2 = displayFeatures(task.leftView.Clone, leftPoints)
        dst3 = displayFeatures(task.rightView.Clone, rightPoints)

        Dim threshold = Math.Min(gOptions.FrameHistory.Value, leftHist.Count)
        leftHist.Add(tmpLeft)
        rightHist.Add(tmpRight)

        If leftHist.Count >= gOptions.FrameHistory.Value Then leftHist.RemoveAt(0)
        If rightHist.Count >= gOptions.FrameHistory.Value Then rightHist.RemoveAt(0)

        If task.heartBeat Then
            labels(2) = CStr(leftPoints.Count) + " detected in the left image that have matches in " + CStr(threshold) + " previous frames"
            labels(3) = CStr(rightPoints.Count) + " detected in the right image that have matches in " + CStr(threshold) + " previous frames"
        End If
    End Sub
End Class





