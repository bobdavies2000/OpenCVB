Imports cv = OpenCvSharp
Public Class FeatureMatch_Basics : Inherits VB_Algorithm
    Dim lrFeat As New FeatureMatch_LeftRight
    Public mpList As New List(Of pointPair)
    Public mpCorrelation As New List(Of Single)
    Public pad As Integer, gsize As Integer, correlationMin As Single
    Public selectedPoint As cv.Point
    Dim clickPoint As cv.Point, picTag As Integer
    Public Sub New()
        task.mouseClickFlag = True
        task.clickPoint = New cv.Point(dst2.Width / 2, dst2.Height / 2)
        gOptions.MaxDepth.Value = 20
        task.mousePicTag = 2
        If standalone Then gOptions.displayDst1.Checked = True
        labels(1) = "NOTE: matching right point is always to the left of the left point"
        desc = "Identify which feature in the left image corresponds to the feature in the right image."
    End Sub
    Public Sub buildCorrelations(leftFeatures As List(Of List(Of cv.Point)), rightFeatures As List(Of List(Of cv.Point)))
        Static corrSlider = findSlider("Feature Correlation Threshold")
        Static cellSlider = findSlider("MatchTemplate Cell Size")
        pad = CInt(cellSlider.value / 2)
        gsize = cellSlider.value
        correlationMin = corrSlider.value / 100

        Dim correlationmat As New cv.Mat
        mpList.Clear()
        mpCorrelation.Clear()
        For i = 0 To leftFeatures.Count - 1
            For Each pt In leftFeatures(i)
                Dim rect = validateRect(New cv.Rect(pt.X - pad, pt.Y - pad, gsize, gsize))
                Dim correlations As New List(Of Single)
                For Each ptRight In rightFeatures(i)
                    Dim r = validateRect(New cv.Rect(ptRight.X - pad, ptRight.Y - pad, gsize, gsize))
                    cv.Cv2.MatchTemplate(task.leftView(rect), task.rightView(r), correlationmat, cv.TemplateMatchModes.CCoeffNormed)
                    correlations.Add(correlationmat.Get(Of Single)(0, 0))
                Next
                Dim maxCorrelation = correlations.Max
                If maxCorrelation >= correlationMin Then
                    Dim index = correlations.IndexOf(maxCorrelation)
                    mpList.Add(New pointPair(pt, rightFeatures(i)(index)))
                    mpCorrelation.Add(maxCorrelation)
                End If
            Next
        Next
    End Sub
    Private Sub setClickPoint(pt As cv.Point, _pictag As Integer)
        clickPoint = pt
        picTag = _pictag
        task.drawRect = New cv.Rect(clickPoint.X - pad, clickPoint.Y - pad, gsize, gsize)
        task.drawRectUpdated = True
    End Sub
    Public Sub displayResults()
        dst2 = task.leftView
        dst3 = task.rightView
        For Each mp In mpList
            dst2.Circle(mp.p1, task.dotSize, task.highlightColor, -1, task.lineType)
            dst3.Circle(mp.p2, task.dotSize, task.highlightColor, -1, task.lineType)
        Next

        If task.mouseClickFlag Then setClickPoint(task.clickPoint, task.mousePicTag)

        setTrueText("Click near any feature to find the corresponding pair of features.", 1)
        If mpList.Count > 0 And clickPoint <> newPoint Then
            Static knn As New KNN_Core
            knn.queries.Clear()
            knn.queries.Add(task.clickPoint)

            Dim mp As pointPair
            knn.trainInput.Clear()
            For Each mp In mpList
                Dim pt = If(picTag = 2, mp.p1, mp.p2)
                knn.trainInput.Add(New cv.Point2f(pt.X, pt.Y))
            Next
            knn.Run(Nothing)

            dst1.SetTo(0)
            Dim mpIndex = knn.result(0, 0)
            mp = mpList(mpIndex)

            If firstPass Then setClickPoint(mp.p1, 2)

            dst2.Circle(mp.p1, task.dotSize + 4, cv.Scalar.Red, -1, task.lineType)
            dst3.Circle(mp.p2, task.dotSize + 4, cv.Scalar.Red, -1, task.lineType)

            Dim dspDistance = task.pcSplit(2).Get(Of Single)(mp.p1.Y, mp.p1.X)

            Dim offset = mp.p1.X - mp.p2.X
            strOut = Format(mpCorrelation(mpIndex), fmt3) + vbCrLf + Format(dspDistance, fmt3) + "m (from camera)" + vbCrLf +
                        CStr(offset) + " Pixel difference"

            If task.heartBeat Then dst1.SetTo(0)
            dst1.Circle(mp.p1, task.dotSize, task.highlightColor, -1, task.lineType)
            dst1.Circle(mp.p2, task.dotSize, task.highlightColor, -1, task.lineType)

            selectedPoint = New cv.Point(mp.p1.X, mpList(mpIndex).p1.Y + 10)
            setTrueText(strOut, selectedPoint, 1)
            If task.heartBeat Then
                labels(2) = CStr(mpList.Count) + " features matched and confirmed with left/right image correlation coefficients"
            End If
        End If
    End Sub
    Public Sub RunVB(src As cv.Mat)
        lrFeat.Run(src)
        labels(3) = lrFeat.labels(3)

        buildCorrelations(lrFeat.leftFeatures, lrFeat.rightFeatures)
        displayResults()
    End Sub
End Class









Public Class FeatureMatch_LeftRightHist : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim fGrid As New Feature_Grid
    Public leftPoints As New List(Of cv.Point)
    Public rightPoints As New List(Of cv.Point)
    Public Sub New()
        If findfrm(traceName + " Radio Buttons") Is Nothing Then
            radio.Setup(traceName)
            radio.addRadio("Use image best features")
            radio.addRadio("Use grid best features")
            radio.check(0).Checked = True
        End If

        findSlider("Min Distance to next").Value = 1
        gOptions.FrameHistory.Value = 10
        If task.workingRes.Width > 336 Then gOptions.FrameHistory.Value = 1
        desc = "Keep only the features that have been around for the specified number of frames."
    End Sub
    Public Function displayFeatures(dst As cv.Mat, features As List(Of cv.Point)) As cv.Mat
        For Each pt In features
            dst.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        Return dst
    End Function
    Public Sub RunVB(src As cv.Mat)
        Static gridRadio = findRadio("Use grid best features")
        Dim gridFeatures = gridRadio.checked
        Dim minPoints = 10

        If gridFeatures Then fGrid.Run(task.leftView) Else feat.Run(task.leftView)
        Dim tmpLeft As New List(Of cv.Point)
        For Each pt In task.features
            tmpLeft.Add(New cv.Point(pt.X, pt.Y))
        Next

        If gridFeatures Then fGrid.Run(task.rightView) Else feat.Run(task.rightView)
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
        If rightPoints.Count < minPoints Then
            rightPoints = tmpRight
            rightHist = New List(Of List(Of cv.Point))({tmpRight})
        End If

        dst2 = displayFeatures(task.leftView.Clone, leftPoints)
        dst3 = displayFeatures(task.rightView.Clone, rightPoints)

        leftHist.Add(tmpLeft)
        rightHist.Add(tmpRight)
        Dim threshold = Math.Min(gOptions.FrameHistory.Value, leftHist.Count)

        If leftHist.Count >= gOptions.FrameHistory.Value Then leftHist.RemoveAt(0)
        If rightHist.Count >= gOptions.FrameHistory.Value Then rightHist.RemoveAt(0)

        If task.heartBeat Then
            labels(2) = CStr(leftPoints.Count) + " detected in the left image that have matches in " + CStr(threshold) + " previous left images"
            labels(3) = CStr(rightPoints.Count) + " detected in the right image that have matches in " + CStr(threshold) + " previous right images"
        End If
    End Sub
End Class




Public Class FeatureMatch_BasicsOld : Inherits VB_Algorithm
    Public feat As New FeatureMatch_LeftRightOld
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






Public Class FeatureMatch_LeftRightOld : Inherits VB_Algorithm
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






Public Class FeatureMatch_Grid : Inherits VB_Algorithm
    Dim fGrid As New Feature_Grid
    Public Sub New()
        desc = "Gather Feature_Grid points for the left and right images - they will be spread across the entire image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        fGrid.Run(task.leftView)
        dst2 = fGrid.dst2.Clone
        If task.heartBeat Then labels(2) = fGrid.labels(2)

        fGrid.Run(task.rightView)
        dst3 = fGrid.dst2.Clone
        If task.heartBeat Then labels(3) = fGrid.labels(2)
    End Sub
End Class






Public Class FeatureMatch_Delaunay : Inherits VB_Algorithm
    Dim facet As New Delaunay_Contours
    Dim fMatch As New FeatureMatch_Basics
    Public Sub New()
        findSlider("Min Distance to next").Value = 10
        desc = "Divide the image with Delaunay using the features found in Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.heartBeat Or facet.inputPoints.Count <= 10 Then
            fMatch.Run(src)
            dst2 = fMatch.dst2
            labels(2) = fMatch.labels(2)

            facet.inputPoints.Clear()
            For Each mp In fMatch.mpList
                facet.inputPoints.Add(mp.p1)
            Next

            facet.Run(src)
            dst3 = facet.dst2
        Else
            dst2 = src
            For Each pt In facet.inputPoints
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            Next
        End If

    End Sub
End Class






Public Class FeatureMatch_LeftRight : Inherits VB_Algorithm
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
                ptlist = New List(Of cv.Point)({pt})
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
                ptlist = New List(Of cv.Point)({pt})
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