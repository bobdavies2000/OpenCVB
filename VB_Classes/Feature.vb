Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Feature_Basics : Inherits VB_Algorithm
    Dim matList As New List(Of cv.Mat)
    Dim ptList As New List(Of cv.Point2f)
    Dim knn As New KNN_Core
    Dim ptLost As New List(Of cv.Point2f)
    Dim gather As New Feature_Gather
    Public options As New Options_Features
    Public Sub New()
        task.features.Clear() ' in case it was previously in use...
        desc = "Identify features with GoodFeaturesToTrack but manage them with MatchTemplate"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static featureMat As New List(Of cv.Mat)
        gather.Run(src)

        If task.optionsChanged Then
            task.features.Clear()
            featureMat.Clear()
        End If

        matList.Clear()
        ptList.Clear()
        Dim correlationMat As New cv.Mat
        For i = 0 To Math.Min(featureMat.Count, task.features.Count) - 1
            Dim pt = task.features(i)
            Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMat(i).Width, featureMat(i).Height))
            If gather.ptList.Contains(pt) = false Then
                cv.Cv2.MatchTemplate(src(rect), featureMat(i), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                If correlationMat.Get(Of Single)(0, 0) < options.correlationMin Then
                    Dim ptNew = New cv.Point2f(CInt(pt.X), CInt(pt.Y))
                    If ptLost.Contains(ptNew) = False Then ptLost.Add(ptNew)
                    Continue For
                End If
            End If
            matList.Add(featureMat(i))
            ptList.Add(pt)
            ' setTrueText(Format(correlationMat.Get(Of Single)(0, 0), fmt1), pt)
        Next

        featureMat = New List(Of cv.Mat)(matList)
        task.features = New List(Of cv.Point2f)(ptList)

        Dim extra = 1 + (1 - options.resyncThreshold)
        task.featureMotion = True

        If task.features.Count < gather.features.Count * options.resyncThreshold Or task.features.Count > extra * gather.features.Count Then
            featureMat.Clear()
            task.features.Clear()
            For Each pt In gather.features
                Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                featureMat.Add(src(rect))
                task.features.Add(pt)
            Next
        Else
            If ptLost.Count > 0 Then
                knn.queries = ptLost
                knn.trainInput = gather.features
                knn.Run(Nothing)

                For i = 0 To knn.queries.Count - 1
                    Dim pt = knn.queries(i)
                    Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                    featureMat.Add(src(rect))
                    task.features.Add(knn.trainInput(knn.result(i, 0)))
                Next
            Else
                task.featureMotion = False
            End If
        End If

        task.featurePoints.Clear()
        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))
        Next
        labels(2) = CStr(task.features.Count) + " features " + CStr(matList.Count) + " were matched to the previous frame using correlation and " +
                    CStr(ptLost.Count) + " features had to be relocated."
        ptLost.Clear()
    End Sub
End Class







' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_BasicsNoFrills : Inherits VB_Algorithm
    Public options As New Options_Features
    Dim gather As New Feature_Gather
    Public Sub New()
        vbAddAdvice(traceName + ": Use 'Options_Features' to control output.")
        desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone

        gather.Run(src)

        task.features.Clear()
        task.featurePoints.Clear()
        For Each pt In gather.features
            task.features.Add(pt)
            task.featurePoints.Add(New cv.Point(pt.X, pt.X))
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next

        labels(2) = gather.labels(2)
    End Sub
End Class






' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_KNN : Inherits VB_Algorithm
    Dim knn As New KNN_Core
    Public featurePoints As New List(Of cv.Point2f)
    Public feat As New Feature_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find good features to track in a BGR image but use the same point if closer than a threshold"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)

        knn.queries = New List(Of cv.Point2f)(task.features)
        If firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(empty)

        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = task.features(i)
            If pt.DistanceTo(qPt) > feat.options.minDistance Then knn.trainInput(trainIndex) = task.features(i)
        Next
        featurePoints = New List(Of cv.Point2f)(knn.trainInput)

        src.CopyTo(dst2)
        dst3.SetTo(0)
        For Each pt In featurePoints
            dst2.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(pt, task.dotSize + 2, cv.Scalar.White, -1, task.lineType)
        Next

        labels(2) = feat.labels(2)
        labels(3) = feat.labels(2)
    End Sub
End Class







Public Class Feature_tCellTracker : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Dim tracker As New Feature_Points
    Dim match As New Match_tCell
    Public tcells As New List(Of tCell)
    Dim options As New Options_Features
    Public Sub New()
        flow.dst = RESULT_DST3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X regions with goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim correlationMin = options.correlationMin

        strOut = ""
        If tcells.Count < task.features.Count / 3 Or tcells.Count < 2 Or task.optionsChanged Then
            tracker.Run(src)
            tcells.Clear()
            For Each pt In task.features
                tcells.Add(match.createCell(src, 0, pt))
            Next
            strOut += "------------------" + vbCrLf + vbCrLf
        End If

        dst2 = src.Clone

        Dim newCells As New List(Of tCell)
        For Each tc In tcells
            match.tCells(0) = tc
            match.Run(src)
            If match.tCells(0).correlation >= correlationMin Then
                tc = match.tCells(0)
                setTrueText(Format(tc.correlation, fmt3), tc.center)
                If standaloneTest() Then strOut += Format(tc.correlation, fmt3) + ", "
                dst2.Circle(tc.center, task.dotSize, task.highlightColor, -1, task.lineType)
                dst2.Rectangle(tc.rect, task.highlightColor, task.lineWidth, task.lineType)
                newCells.Add(tc)
            End If
        Next

        If standaloneTest() Then
            flow.msgs.Add(strOut)
            flow.Run(empty)
        End If

        tcells = New List(Of tCell)(newCells)
        labels(2) = "Of the " + CStr(task.features.Count) + " input cells " + CStr(newCells.Count) + " cells were tracked with correlation above " +
                    Format(correlationMin, fmt1)
    End Sub
End Class






Public Class Feature_Reduction : Inherits VB_Algorithm
    Dim reduction As New Reduction_Basics
    Dim feat As New Feature_Basics
    Public Sub New()
        labels = {"", "", "Good features", "History of good features"}
        desc = "Get the features in a reduction grayscale image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)
        dst2 = src

        feat.Run(reduction.dst2)
        If task.heartBeat Then dst3.SetTo(0)
        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
            dst3.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next
    End Sub
End Class







Public Class Feature_MultiPass : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Public featurePoints As New List(Of cv.Point2f)
    Dim sharpen As New PhotoShop_SharpenDetail
    Public Sub New()
        gOptions.RGBFilterActive.Checked = True
        gOptions.RGBFilterList.SelectedIndex = gOptions.RGBFilterList.Items.IndexOf("Filter_Laplacian")
        desc = "Run Feature_Basics twice and compare results."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(task.color)
        dst2 = src.Clone
        featurePoints = New List(Of cv.Point2f)(task.features)
        Dim passCounts As String = CStr(featurePoints.Count) + "/"

        feat.Run(src)
        For Each pt In task.features
            featurePoints.Add(pt)
        Next
        passCounts += CStr(task.features.Count) + "/"

        sharpen.Run(task.color)
        feat.Run(sharpen.dst2)
        For Each pt In task.features
            featurePoints.Add(pt)
        Next
        passCounts += CStr(task.features.Count)

        For Each pt In featurePoints
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        If task.heartBeat Then
            labels(2) = "Total features = " + CStr(featurePoints.Count) + ", pass counts = " + passCounts
        End If
    End Sub
End Class








Public Class Feature_PointTracker : Inherits VB_Algorithm
    Dim flow As New Font_FlowText
    Public feat As New Feature_Basics
    Dim mPoints As New Match_Points
    Dim options As New Options_Features
    Public Sub New()
        flow.dst = RESULT_DST3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        Dim correlationMin = options.correlationMin
        Dim templatePad = options.templatePad
        Dim templateSize = options.templateSize

        strOut = ""
        If mPoints.ptx.Count <= 3 Then
            mPoints.ptx.Clear()
            feat.Run(src)
            For Each pt In task.features
                mPoints.ptx.Add(pt)
                Dim rect = validateRect(New cv.Rect(pt.X - templatePad, pt.Y - templatePad, templateSize, templateSize))
            Next
            strOut = "Restart tracking -----------------------------------------------------------------------------" + vbCrLf
        End If
        mPoints.Run(src)

        dst2 = src.Clone
        For i = mPoints.ptx.Count - 1 To 0 Step -1
            If mPoints.correlation(i) > correlationMin Then
                dst2.Circle(mPoints.ptx(i), task.dotSize, task.highlightColor, -1, task.lineType)
                strOut += Format(mPoints.correlation(i), fmt3) + ", "
            Else
                mPoints.ptx.RemoveAt(i)
            End If
        Next
        If standaloneTest() Then
            flow.msgs.Add(strOut)
            flow.Run(empty)
        End If

        labels(2) = "Of the " + CStr(task.features.Count) + " input points, " + CStr(mPoints.ptx.Count) +
                    " points were tracked with correlation above " + Format(correlationMin, fmt2)
    End Sub
End Class






Public Class Feature_LongestKNN : Inherits VB_Algorithm
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match As New Match_Basics
    Public Sub New()
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src

        Static p1 As cv.Point, p2 As cv.Point
        knn.Run(src.Clone)
        p1 = knn.lastPair.p1
        p2 = knn.lastPair.p2
        gline = glines.updateGLine(src, gline, p1, p2)

        Dim rect = validateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
        match.template = src(rect)
        match.Run(src)
        If match.correlation >= options.correlationMin Then
            dst3 = match.dst0.Resize(dst3.Size)
            dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
            dst2.Circle(p1, task.dotSize, task.highlightColor, -1, task.lineType)
            dst2.Circle(p2, task.dotSize, task.highlightColor, -1, task.lineType)
            rect = validateRect(New cv.Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X) + 2, Math.Abs(p1.Y - p2.Y)))
            match.template = src(rect).Clone
        Else
            task.highlightColor = If(task.highlightColor = cv.Scalar.Yellow, cv.Scalar.Blue, cv.Scalar.Yellow)
            knn.lastPair = New pointPair(New cv.Point2f, New cv.Point2f)
        End If
        labels(2) = "Longest line end points had correlation of " + Format(match.correlation, fmt3) + " with the original longest line."
    End Sub
End Class






Public Class Feature_Longest : Inherits VB_Algorithm
    Dim glines As New Line_GCloud
    Public knn As New KNN_ClosestTracker
    Public options As New Options_Features
    Public gline As gravityLine
    Public match1 As New Match_Basics
    Public match2 As New Match_Basics
    Public Sub New()
        labels(2) = "Longest line end points are highlighted "
        desc = "Find and track the longest line in the BGR image with a lightweight KNN."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        Dim correlationMin = match1.options.correlationMin
        Dim templatePad = match1.options.templatePad
        Dim templateSize = match1.options.templateSize

        Static p1 As cv.Point, p2 As cv.Point
        If task.heartBeat Or match1.correlation < correlationMin And match2.correlation < correlationMin Then
            knn.Run(src.Clone)

            p1 = knn.lastPair.p1
            Dim r1 = validateRect(New cv.Rect(p1.X - templatePad, p1.Y - templatePad, templateSize, templateSize))
            match1.template = src(r1).Clone

            p2 = knn.lastPair.p2
            Dim r2 = validateRect(New cv.Rect(p2.X - templatePad, p2.Y - templatePad, templateSize, templateSize))
            match2.template = src(r2).Clone
        End If

        match1.Run(src)
        p1 = match1.matchCenter

        match2.Run(src)
        p2 = match2.matchCenter

        gline = glines.updateGLine(src, gline, p1, p2)
        dst2.Line(p1, p2, task.highlightColor, task.lineWidth, task.lineType)
        dst2.Circle(p1, task.dotSize, task.highlightColor, -1, task.lineType)
        dst2.Circle(p2, task.dotSize, task.highlightColor, -1, task.lineType)
        setTrueText(Format(match1.correlation, fmt3), p1)
        setTrueText(Format(match2.correlation, fmt3), p2)
    End Sub
End Class







Public Class Feature_Delaunay : Inherits VB_Algorithm
    Dim facet As New Delaunay_Contours
    Dim feat As New Feature_Basics
    Public Sub New()
        findSlider("Min Distance to next").Value = 10
        desc = "Divide the image into contours with Delaunay using features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

        facet.inputPoints.Clear()
        For Each pt In task.features
            facet.inputPoints.Add(pt)
        Next

        facet.Run(src)
        dst3 = facet.dst2
        For Each pt In task.features
            dst3.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next
        labels(3) = "There were " + CStr(task.features.Count) + " Delaunay contours"
    End Sub
End Class







Public Class Feature_LucasKanade : Inherits VB_Algorithm
    Dim pyr As New FeatureFlow_LucasKanade
    Public ptList As New List(Of cv.Point)
    Public ptLast As New List(Of cv.Point)
    Public Sub New()
        desc = "Provide a trace of the tracked features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static ptHist As New List(Of List(Of cv.Point))
        pyr.Run(src)
        dst2 = src
        labels(2) = pyr.labels(2)

        If task.heartBeat Then dst3.SetTo(0)

        ptList.Clear()
        Dim stationary As Integer, motion As Integer
        For i = 0 To pyr.features.Count - 1
            Dim pt = New cv.Point(pyr.features(i).X, pyr.features(i).Y)
            ptList.Add(pt)
            If ptLast.Contains(pt) Then
                dst3.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                stationary += 1
            Else
                dst3.Line(pyr.lastFeatures(i), pyr.features(i), cv.Scalar.White, task.lineWidth, task.lineType)
                motion += 1
            End If
        Next

        If task.heartBeat Then labels(3) = CStr(stationary) + " features were stationary and " + CStr(motion) + " features had some motion."
        ptLast = New List(Of cv.Point)(ptList)
    End Sub
End Class







' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_GridSimple : Inherits VB_Algorithm
    Public options As New Options_Features
    Public Sub New()
        findSlider("Feature Sample Size").Value = 1
        desc = "Find good features to track in each roi of the task.gridList"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        dst2 = src.Clone
        options.RunVB()

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        task.features.Clear()
        For Each roi In task.gridList
            Dim features = cv.Cv2.GoodFeaturesToTrack(src(roi), options.featurePoints, options.quality, options.minDistance, Nothing,
                                                      options.blockSize, True, options.k)
            For Each pt In features
                task.features.Add(New cv.Point2f(roi.X + pt.X, roi.Y + pt.Y))
            Next
        Next

        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        labels(2) = "Found " + CStr(task.features.Count) + " points with quality = " + CStr(options.quality) +
                    " and minimum distance = " + CStr(options.minDistance) + " and blocksize " + CStr(options.blockSize)
    End Sub
End Class





Public Class Feature_Grid : Inherits VB_Algorithm
    Dim options As New Options_Features
    Dim matList As New List(Of cv.Mat)
    Dim ptList As New List(Of cv.Point2f)
    Dim knn As New KNN_Core
    Dim ptLost As New List(Of cv.Point2f)
    Dim gather As New Feature_Gather
    Public Sub New()
        findRadio("GoodFeatures (ShiTomasi) grid").Checked = True
        findSlider("Feature Sample Size").Value = 4
        desc = "Find good features to track in each roi of the task.gridList"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static featureMat As New List(Of cv.Mat)
        If task.optionsChanged Then task.features.Clear()

        matList.Clear()
        ptList.Clear()
        ptLost.Clear()
        Dim correlationMat As New cv.Mat
        For i = 0 To task.features.Count - 1
            Dim pt = task.features(i)
            Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMat(i).Width, featureMat(i).Height))
            cv.Cv2.MatchTemplate(src(rect), featureMat(i), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
            If correlationMat.Get(Of Single)(0, 0) > options.correlationMin Then
                matList.Add(featureMat(i))
                ptList.Add(pt)
            Else
                ptLost.Add(pt)
            End If
        Next

        featureMat = New List(Of cv.Mat)(matList)
        task.features = New List(Of cv.Point2f)(ptList)

        gather.Run(src)
        Dim nextFeatures = gather.features

        If task.features.Count < nextFeatures.Count * options.resyncThreshold Then
            featureMat.Clear()
            task.features.Clear()
            For Each pt In nextFeatures
                Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                featureMat.Add(src(rect))
                task.features.Add(pt)
            Next
        Else
            knn.queries = ptLost
            knn.trainInput = nextFeatures
            knn.Run(Nothing)

            For i = 0 To knn.queries.Count - 1
                Dim pt = knn.queries(i)
                Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                featureMat.Add(src(rect))
                task.features.Add(knn.trainInput(knn.result(i, 0)))
            Next
        End If

        task.featurePoints.Clear()
        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))
        Next
        labels(2) = CStr(task.features.Count) + " features " + CStr(matList.Count) + " were matched using correlation coefficients and " +
                    CStr(ptLost.Count) + " features had to be relocated."
    End Sub
End Class








Public Class Feature_NearestCell : Inherits VB_Algorithm
    Dim redC As New RedCloud_Basics
    Dim feat As New FeatureLeftRight_Basics
    Dim knn As New KNN_Core
    Public Sub New()
        desc = "Find the nearest feature to every cell in task.redCells"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        redC.Run(src)
        dst2 = redC.dst2
        dst3 = redC.dst2.Clone
        labels(2) = redC.labels(2)

        knn.queries.Clear()
        For Each rc In task.redCells
            knn.queries.Add(rc.maxDStable)
        Next

        knn.trainInput.Clear()
        For Each mp In feat.mpList
            knn.trainInput.Add(New cv.Point2f(mp.p1.X, mp.p1.Y))
        Next

        knn.Run(Nothing)

        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            rc.nearestFeature = knn.trainInput(knn.result(i, 0))
            dst3.Line(rc.nearestFeature, rc.maxDStable, task.highlightColor, task.lineWidth, task.lineType)
        Next
    End Sub
End Class








Public Class Feature_Points : Inherits VB_Algorithm
    Public feat As New Feature_Basics
    Public Sub New()
        labels(3) = "Features found in the image"
        desc = "Use the sorted list of Delaunay regions to find the top X points to track."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        If task.heartBeat Then dst3.SetTo(0)

        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
            dst3.Circle(pt, task.dotSize, task.highlightColor, task.lineWidth, task.lineType)
        Next
        labels(2) = CStr(task.features.Count) + " targets were present with " + CStr(feat.options.featurePoints) + " requested."
    End Sub
End Class






Public Class Feature_Trace : Inherits VB_Algorithm
    Dim track As New RedTrack_Features
    Public Sub New()
        desc = "Placeholder to help find RedTrack_Features"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        track.Run(src)
        dst2 = track.dst2
        labels = track.labels
    End Sub
End Class





Public Class Feature_TraceDelaunay : Inherits VB_Algorithm
    Dim features As New Feature_Delaunay
    Public goodList As New List(Of List(Of cv.Point2f)) ' stable points only
    Public Sub New()
        labels = {"Stable points highlighted", "", "", "Delaunay map of regions defined by the feature points"}
        desc = "Trace the GoodFeatures points using only Delaunay - no KNN or RedCloud or Matching."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        features.Run(src)
        dst3 = features.dst2

        If task.optionsChanged Then goodList.Clear()

        Dim ptList As New List(Of cv.Point2f)(task.features)
        goodList.Add(ptList)

        If goodList.Count >= task.frameHistoryCount Then goodList.RemoveAt(0)

        dst2.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                task.color.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                Dim c = dst3.Get(Of cv.Vec3b)(pt.Y, pt.X)
                dst2.Circle(pt, task.dotSize + 1, c, -1, task.lineType)
            Next
        Next
        labels(2) = CStr(task.features.Count) + " features were identified in the image."
    End Sub
End Class






Public Class Feature_ShiTomasi : Inherits VB_Algorithm
    Dim harris As New Corners_HarrisDetector
    Dim shiTomasi As New Corners_ShiTomasi_CPP
    Dim options As New Options_ShiTomasi
    Public Sub New()
        findSlider("Corner normalize threshold").Value = 15
        labels = {"", "", "Features in the left camera image", "Features in the right camera image"}
        desc = "Identify feature points in the left And right views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If options.useShiTomasi Then
            dst2 = task.leftView
            dst3 = task.rightView
            shiTomasi.Run(task.leftView)
            dst2.SetTo(cv.Scalar.White, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

            shiTomasi.Run(task.rightView)
            dst3.SetTo(task.highlightColor, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Else
            harris.Run(task.leftView)
            dst2 = harris.dst2.Clone
            harris.Run(task.rightView)
            dst3 = harris.dst2
        End If
    End Sub
End Class






Public Class Feature_Generations : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim features As New List(Of cv.Point)
    Dim gens As New List(Of Integer)
    Public Sub New()
        vbAddAdvice(traceName + ": Local options will determine how many features are present.")
        desc = "Find feature age maximum and average."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)

        Dim newfeatures As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
        For Each pt In task.featurePoints
            Dim index = features.IndexOf(pt)
            If index >= 0 Then newfeatures.Add(gens(index) + 1, pt) Else newfeatures.Add(1, pt)
        Next

        If task.heartBeat Then
            features.Clear()
            gens.Clear()
        End If

        features = New List(Of cv.Point)(newfeatures.Values)
        gens = New List(Of Integer)(newfeatures.Keys)

        dst2 = src
        For i = 0 To features.Count - 1
            If gens(i) = 1 Then Exit For
            Dim pt = features(i)
            dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
        Next

        If task.heartBeat Then
            labels(2) = CStr(features.Count) + " features found with max/average " + CStr(gens(0)) + "/" + Format(gens.Average, fmt0) + " generations"
        End If
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_History : Inherits VB_Algorithm
    Public features As New List(Of cv.Point)
    Public feat As New Feature_Basics
    Public Sub New()
        desc = "Find good features across multiple frames."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static featureHistory As New List(Of List(Of cv.Point))
        Static gens As New List(Of Integer)
        Dim histCount = gOptions.FrameHistory.Value

        feat.Run(src)
        dst2 = src.Clone

        featureHistory.Add(New List(Of cv.Point)(task.featurePoints))

        Dim newFeatures As New List(Of cv.Point)
        gens.Clear()
        For Each cList In featureHistory
            For Each pt In cList
                Dim index = newFeatures.IndexOf(pt)
                If index >= 0 Then
                    gens(index) += 1
                Else
                    newFeatures.Add(pt)
                    gens.Add(1)
                End If
            Next
        Next

        Dim threshold = If(histCount = 1, 0, 1)
        features.Clear()
        Dim whiteCount As Integer
        For i = 0 To newFeatures.Count - 1
            If gens(i) > threshold Then
                Dim pt = newFeatures(i)
                features.Add(pt)
                If gens(i) < histCount Then
                    dst2.Circle(pt, task.dotSize + 2, cv.Scalar.Red, -1, task.lineType)
                Else
                    whiteCount += 1
                    dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
                End If
            End If
        Next

        If featureHistory.Count > histCount Then featureHistory.RemoveAt(0)
        If task.heartBeat Then
            labels(2) = CStr(features.Count) + "/" + CStr(whiteCount) + " present/present on every frame" +
                        " Red is a recent addition, yellow is present on previous " + CStr(histCount) + " frames"
        End If
    End Sub
End Class






Public Class Feature_GridPopulation : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Click 'Show grid mask overlay' to see grid boundaries."
        desc = "Find the feature population for each cell."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

        dst3.SetTo(0)
        For Each pt In task.featurePoints
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next

        For Each roi In task.gridList
            Dim test = dst3(roi).FindNonZero()
            setTrueText(CStr(test.Rows), roi.TopLeft, 3)
        Next
    End Sub
End Class










Public Class Feature_Compare : Inherits VB_Algorithm
    Dim feat As New Feature_Basics
    Dim noFrill As New Feature_BasicsNoFrills
    Public Sub New()
        desc = "Prepare features for the left and right views"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static saveLFeatures As New List(Of cv.Point2f)
        Static saveRFeatures As New List(Of cv.Point2f)

        task.features = New List(Of cv.Point2f)(saveLFeatures)
        feat.Run(src.Clone)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)
        saveLFeatures = New List(Of cv.Point2f)(task.features)

        task.features = New List(Of cv.Point2f)(saveRFeatures)
        noFrill.Run(src.Clone)
        dst3 = noFrill.dst2
        labels(3) = "With no correlation coefficients " + noFrill.labels(2)
        saveRFeatures = New List(Of cv.Point2f)(task.features)
    End Sub
End Class






Public Class Feature_Gather : Inherits VB_Algorithm
    Dim harris As New Corners_HarrisDetector
    Dim FAST As New Corners_Basics
    Dim myOptions As New Options_FeatureGather
    Public features As New List(Of cv.Point2f)
    Public ptList As New List(Of cv.Point)
    Dim brisk As New BRISK_Basics
    Public options As New Options_Features
    Public Sub New()
        cPtr = Agast_Open()
        desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        myOptions.RunVB()
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Select Case myOptions.featureSource
            Case FeatureSrc.goodFeaturesFull
                Static sampleSlider = findSlider("Feature Sample Size")
                sampleSlider.value = 400
                features = cv.Cv2.GoodFeaturesToTrack(src, options.featurePoints, options.quality, options.minDistance, New cv.Mat,
                                                      options.blockSize, True, options.k).ToList
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.goodFeaturesGrid
                options.featurePoints = 4
                features.Clear()
                For i = 0 To task.gridList.Count - 1
                    Dim roi = task.gridList(i)
                    Dim tmpFeatures = cv.Cv2.GoodFeaturesToTrack(src(roi), options.featurePoints, options.quality, options.minDistance, New cv.Mat,
                                                                 options.blockSize, True, options.k).ToList
                    For j = 0 To tmpFeatures.Count - 1
                        features.Add(New cv.Point2f(tmpFeatures(j).X + roi.X, tmpFeatures(j).Y + roi.Y))
                    Next
                Next
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.Agast
                src = task.color.Clone
                Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
                Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)

                Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
                Dim imagePtr = Agast_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.agastThreshold)
                handleSrc.Free()

                Dim ptMat = New cv.Mat(Agast_Count(cPtr), 1, cv.MatType.CV_32FC2, imagePtr).Clone
                features.Clear()
                If standaloneTest() Then dst2 = src

                For i = 0 To ptMat.Rows - 1
                    Dim pt = ptMat.Get(Of cv.Point2f)(i, 0)
                    features.Add(pt)
                    If standaloneTest() Then dst2.Circle(pt, task.dotSize, cv.Scalar.White, -1, task.lineType)
                Next

                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.BRISK
                brisk.Run(src)
                features = brisk.features
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.Harris
                harris.Run(src)
                features = harris.features
                labels(2) = "Harris Detector produced " + CStr(features.Count) + " features"
            Case FeatureSrc.FAST
                FAST.Run(src)
                features = FAST.features
                labels(2) = "FAST produced " + CStr(features.Count) + " features"
        End Select

        ptList.Clear()
        For Each pt In features
            ptList.Add(New cv.Point(pt.X, pt.Y))
        Next
        If standaloneTest() Then
            dst2 = task.color.Clone
            For Each pt In features
                dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            Next
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Agast_Close(cPtr)
    End Sub
End Class






'Public Class Feature_BasicsGrid1 : Inherits VB_Algorithm
'    Dim matList As New List(Of cv.Mat)
'    Dim ptList As New List(Of cv.Point2f)
'    Dim gather As New Feature_Gather
'    Public options As New Options_Features
'    Public Sub New()
'        task.features.Clear() ' in case it was previously in use...
'        desc = "Use the grid to determine which cells should refresh their features"
'    End Sub
'    Public Sub RunVB(src As cv.Mat)
'        options.RunVB()
'        dst2 = src.Clone
'        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

'        Static featureMat As New List(Of cv.Mat)

'        If task.optionsChanged Then
'            task.features.Clear()
'            featureMat.Clear()
'        End If

'        matList.Clear()
'        ptList.Clear()
'        Dim correlationMat As New cv.Mat
'        Dim roiLosses As New List(Of Integer)
'        For i = 0 To Math.Min(featureMat.Count, task.features.Count) - 1
'            Dim pt = task.features(i)
'            Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMat(i).Width, featureMat(i).Height))
'            cv.Cv2.MatchTemplate(src(rect), featureMat(i), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
'            If correlationMat.Get(Of Single)(0, 0) > options.correlationMin Then
'                matList.Add(featureMat(i))
'                ptList.Add(pt)
'            Else
'                roiLosses.Add(task.gridMap.Get(Of Integer)(pt.Y, pt.X))
'            End If
'        Next

'        Dim featuresByGrid(task.gridList.Count - 1) As List(Of cv.Point)
'        For Each pt In task.featurePoints
'            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
'            If roiLosses.Contains(index) Then
'                If featuresByGrid(index) Is Nothing Then featuresByGrid(index) = New List(Of cv.Point)
'                featuresByGrid(index).Add(pt)
'            End If
'        Next


'        featureMat = New List(Of cv.Mat)(matList)
'        task.features = New List(Of cv.Point2f)(ptList)

'        gather.Run(src)
'        Dim nextFeatures = gather.features

'        Dim extra = 1 + (1 - options.resyncThreshold)
'        task.featureMotion = True

'        If task.features.Count < nextFeatures.Count * options.resyncThreshold Or task.features.Count > extra * nextFeatures.Count Then
'            featureMat.Clear()
'            task.features.Clear()
'            For Each pt In nextFeatures
'                Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
'                featureMat.Add(src(rect))
'                task.features.Add(pt)
'            Next
'        End If

'        task.featurePoints.Clear()
'        For Each pt In task.features
'            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
'            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))
'        Next
'        labels(2) = CStr(task.features.Count) + " features " + CStr(matList.Count) + " were matched to the previous frame using correlation and " +
'                    CStr(ptLostCount) + " features had to be relocated."
'    End Sub
'End Class







Public Class Feature_BasicsNew : Inherits VB_Algorithm
    Dim knn As New KNN_Core
    Dim gather As New Feature_Gather
    Public options As New Options_Features
    Public Sub New()
        task.features.Clear() ' in case it was previously in use...
        desc = "Identify features with GoodFeaturesToTrack but manage them with MatchTemplate"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        dst2 = src.Clone
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Static featureMat As New List(Of cv.Mat)
        Static featureGrid(task.gridList.Count - 1) As List(Of cv.Point)

        gather.Run(src)

        If task.optionsChanged Then
            task.features.Clear()
            featureMat.Clear()
            ReDim featureGrid(task.gridList.Count - 1)
        End If

        Dim matList As New List(Of cv.Mat)
        Dim ptList As New List(Of cv.Point2f)
        Dim ptLost As New List(Of cv.Point2f)
        Dim correlationMat As New cv.Mat
        For i = 0 To task.featurePoints.Count - 1
            Dim pt = task.featurePoints(i)
            Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMat(i).Width, featureMat(i).Height))
            If gather.ptList.Contains(pt) = False Then
                cv.Cv2.MatchTemplate(src(rect), featureMat(i), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
                If correlationMat.Get(Of Single)(0, 0) < options.correlationMin Then
                    Dim ptNew = New cv.Point2f(CInt(pt.X), CInt(pt.Y))
                    If ptLost.Contains(ptNew) = False Then ptLost.Add(ptNew)
                    Continue For
                End If
            End If
            matList.Add(featureMat(i))
            ptList.Add(pt)
        Next

        'Dim ptLostCount As Integer
        'Dim gridLosses(task.gridList.Count - 1) As List(Of cv.Point)
        'For i = 0 To featuresByGrid.Count - 1
        '    If featuresByGrid(i) Is Nothing Then Continue For
        '    If featuresByGrid(i).Count < 10 Then Continue For
        '    ptLostCount += featuresByGrid(i).Count
        '    For Each pt In featuresByGrid(i)
        '        ptList.Add(pt)
        '        Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMat(i).Width, featureMat(i).Height))
        '        matList.Add(src(rect))

        '    Next
        'Next
        featureMat = New List(Of cv.Mat)(matList)
        task.features = New List(Of cv.Point2f)(ptList)

        task.featureMotion = True
        If task.features.Count < gather.features.Count * options.resyncThreshold Then
            ReDim featureGrid(task.gridList.Count - 1)
            For Each pt In gather.ptList
                Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                If featureGrid(index) Is Nothing Then featureGrid(index) = New List(Of cv.Point)
                featureGrid(index).Add(pt)
            Next

            featureMat.Clear()
            task.features.Clear()
            For Each pt In gather.features
                Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                featureMat.Add(src(rect))
                task.features.Add(pt)
            Next
        Else
            If ptLost.Count > 0 Then
                knn.queries = ptLost
                knn.trainInput = gather.features
                knn.Run(Nothing)

                For i = 0 To knn.queries.Count - 1
                    Dim pt = knn.queries(i)
                    Dim rect = validateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                    featureMat.Add(src(rect))
                    task.features.Add(knn.trainInput(knn.result(i, 0)))
                Next
            Else
                task.featureMotion = False
            End If
        End If

        task.featurePoints.Clear()
        For Each pt In task.features
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))
        Next
        labels(2) = CStr(task.features.Count) + " features " + CStr(matList.Count) + " were matched to the previous frame using correlation and " +
                    CStr(ptLost.Count) + " features had to be relocated."
        ptLost.Clear()
    End Sub
End Class
