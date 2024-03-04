Imports OpenCvSharp.Flann
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
        desc = "Pair left/right features using correlation coefficients"
    End Sub
    Private Sub highlight(mp As pointPair)
        dst2X.Line(mp.p1, New cv.Point2f(mp.p2.X + dst2.Width, mp.p2.Y), cv.Scalar.White, task.lineWidth, task.lineType)
        labels = {"", "The AddWeighted_Basics output showing both the left and right images with corresponding points",
                  "Draw a rectangle anywhere to isolate specific good features", ""}
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

        Dim correlationmat As New cv.Mat, rSize = feat.feat.options.fOptions.matchCellSize, roi = feat.feat.options.roi
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
        For Each pt In feat.featurePoints
            tmpRight.Add(pt)
            ptRight.Add(CInt(pt.Y))
        Next
        dst3 = feat.dst2

        feat.Run(task.leftView)

        Dim tmpLeft As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
        For Each pt In feat.featurePoints
            tmpLeft.Add(pt.Y, pt)
        Next
        dst2 = feat.dst2

        leftCorners.Clear()
        rightCorners.Clear()
        Dim rowList As New List(Of Integer)
        Dim rSize = feat.options.fOptions.matchCellSize
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
            labels(2) = "There were " + CStr(rowList.Count) + " rows with features in both rows."
            labels(3) = "Left image has " + CStr(leftCorners.Count) + " good features and the right camera has " +
                                       CStr(rightCorners.Count) + " good features"
        End If
    End Sub
End Class







Public Class FeatureMatch_History : Inherits VB_Algorithm
    Public feat As New FeatureMatch_Basics
    Public mpList As New List(Of pointPair)
    Public corrList As New List(Of Single)
    Dim matchCount As Integer
    Public Sub New()
        If standaloneTest() Then gOptions.displayDst1.Checked = True
        labels(1) = "The AddWeighted output of the left and right images connecting the corresponding points."
        desc = "Pair left/right features using correlation coefficients"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static thresholdSlider = findSlider("Feature Correlation Threshold")
        Dim minCorr = thresholdSlider.value / 100

        Static allLists As New List(Of List(Of pointPair))
        Static allCorrs As New List(Of List(Of Single))
        If task.optionsChanged Then
            allLists.Clear()
            allCorrs.Clear()
        End If

        feat.Run(empty)

        allLists.Add(New List(Of pointPair)(feat.mpList))
        allCorrs.Add(New List(Of Single)(feat.corrList))

        If allLists.Count > task.frameHistoryCount Then
            allLists.RemoveAt(0)
            allCorrs.RemoveAt(0)
        End If

        mpList.Clear()
        corrList.Clear()

        For i = 0 To allLists.Count - 1
            For Each mp In allLists(i)
                mpList.Add(mp)
                Dim index = allLists(i).IndexOf(mp)
                corrList.Add(allCorrs(i)(index))
            Next
        Next

        feat.showMatches(mpList, corrList)
        dst1 = feat.dst1
        dst2 = feat.dst2
        dst3 = feat.dst3

        trueData = New List(Of trueText)(feat.trueData)
        If task.heartBeat Then
            labels(3) = CStr(mpList.Count) + " points were matched with a minimum " + Format(minCorr, fmt1) + " correlation"
        End If
    End Sub
End Class