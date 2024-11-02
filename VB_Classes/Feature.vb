Imports cvb = OpenCvSharp
Imports System.Runtime.InteropServices
Imports OpenCvSharp
Public Class Feature_Basics : Inherits TaskParent
    Public options As New Options_Features
    Dim gather As New Feature_Gather
    Public Sub New()
        UpdateAdvice(traceName + ": Use 'Options_Features' to control output.")
        desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent results."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        dst2 = src.Clone

        gather.Run(src)

        Static ptList As New List(Of cvb.Point2f)
        If task.FirstPass Then
            ptList = New List(Of cvb.Point2f)(gather.features)
        Else
            Dim newSet As New List(Of cvb.Point2f)
            For Each pt In ptList
                Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 0 Then newSet.Add(pt)
            Next

            For Each pt In gather.features
                Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val <> 0 Then newSet.Add(pt)
            Next
            ptList = New List(Of cvb.Point2f)(newSet)
        End If

        task.features.Clear()
        task.featurePoints.Clear()
        For Each pt In ptList
            task.features.Add(pt)
            task.featurePoints.Add(New cvb.Point(pt.X, pt.X))
        Next

        For Each pt In ptList
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next

        labels(2) = gather.labels(2)
    End Sub
End Class





' https://docs.opencvb.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_NoMotionTest : Inherits TaskParent
    Public options As New Options_Features
    Dim gather As New Feature_Gather
    Public Sub New()
        UpdateAdvice(traceName + ": Use 'Options_Features' to control output.")
        desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent results."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        dst2 = src.Clone

        gather.Run(src)

        task.features.Clear()
        task.featurePoints.Clear()
        For Each pt In gather.features
            task.features.Add(pt)
            task.featurePoints.Add(New cvb.Point(pt.X, pt.X))
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next

        labels(2) = gather.labels(2)
    End Sub
End Class




Public Class Feature_Stable : Inherits TaskParent
    Dim nextMatList As New List(Of cvb.Mat)
    Dim ptList As New List(Of cvb.Point2f)
    Dim knn As New KNN_Basics
    Dim ptLost As New List(Of cvb.Point2f)
    Dim gather As New Feature_Gather
    Dim featureMatList As New List(Of cvb.Mat)
    Public options As New Options_Features
    Dim noMotionFrames As Single
    Public Sub New()
        task.features.Clear() ' in case it was previously in use...
        desc = "Identify features with GoodFeaturesToTrack but manage them with MatchTemplate"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        dst2 = src.Clone
        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        gather.Run(src)

        If task.optionsChanged Then
            task.features.Clear()
            featureMatList.Clear()
        End If

        nextMatList.Clear()
        ptList.Clear()
        Dim correlationMat As New cvb.Mat
        Dim saveFeatureCount = Math.Min(featureMatList.Count, task.features.Count)
        For i = 0 To saveFeatureCount - 1
            Dim pt = task.features(i)
            Dim rect = ValidateRect(New cvb.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMatList(i).Width, featureMatList(i).Height))
            If gather.ptList.Contains(pt) = False Then
                cvb.Cv2.MatchTemplate(src(rect), featureMatList(i), correlationMat, cvb.TemplateMatchModes.CCoeffNormed)
                If correlationMat.Get(Of Single)(0, 0) < options.correlationMin Then
                    Dim ptNew = New cvb.Point2f(CInt(pt.X), CInt(pt.Y))
                    If ptLost.Contains(ptNew) = False Then ptLost.Add(ptNew)
                    Continue For
                End If
            End If
            nextMatList.Add(featureMatList(i))
            ptList.Add(pt)
        Next

        Dim survivorPercent As Single = (ptList.Count - ptLost.Count) / saveFeatureCount
        Dim extra = 1 + (1 - options.resyncThreshold)
        task.features = New List(Of cvb.Point2f)(ptList)

        If task.features.Count < gather.features.Count * options.resyncThreshold Or task.features.Count > extra * gather.features.Count Then
            task.featureMotion = True
            ptLost.Clear()
            featureMatList.Clear()
            task.features.Clear()
            For Each pt In gather.features
                Dim rect = ValidateRect(New cvb.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                featureMatList.Add(src(rect))
                task.features.Add(pt)
            Next
        Else
            If ptLost.Count > 0 Then
                knn.queries = ptLost
                knn.trainInput = gather.features
                knn.Run(Nothing)

                For i = 0 To knn.queries.Count - 1
                    Dim pt = knn.queries(i)
                    Dim rect = ValidateRect(New cvb.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
                    featureMatList.Add(src(rect))
                    task.features.Add(knn.trainInput(knn.result(i, 0)))
                Next
            End If
            task.featureMotion = False
            noMotionFrames += 1
            featureMatList = New List(Of cvb.Mat)(nextMatList)
        End If

        task.featurePoints.Clear()
        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            task.featurePoints.Add(New cvb.Point(pt.X, pt.Y))
        Next
        If task.heartBeat Then
            If task.featureMotion = True Then survivorPercent = 0
            labels(2) = Format(survivorPercent, "0%") + " of " + CStr(task.features.Count) + " features were matched to the previous frame using correlation and " +
                        CStr(ptLost.Count) + " features had to be relocated"
        End If
        If task.heartBeat Then
            Dim percent = noMotionFrames / task.fpsRate
            If percent > 1 Then percent = 1
            labels(3) = CStr(noMotionFrames) + " frames since the last heartbeat with no motion " +
                        " or " + Format(percent, "0%")
            noMotionFrames = 0
        End If
    End Sub
End Class







' https://docs.opencvb.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_KNN : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public featurePoints As New List(Of cvb.Point2f)
    Public feat As New Feature_Stable
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        desc = "Find good features to track in a BGR image but use the same point if closer than a threshold"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)

        knn.queries = New List(Of cvb.Point2f)(task.features)
        If task.FirstPass Then knn.trainInput = New List(Of cvb.Point2f)(knn.queries)
        knn.Run(empty)

        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = task.features(i)
            If pt.DistanceTo(qPt) > feat.options.minDistance Then knn.trainInput(trainIndex) = task.features(i)
        Next
        featurePoints = New List(Of cvb.Point2f)(knn.trainInput)

        src.CopyTo(dst2)
        dst3.SetTo(0)
        For Each pt In featurePoints
            DrawCircle(dst2, pt, task.DotSize + 2, cvb.Scalar.White)
            DrawCircle(dst3, pt, task.DotSize + 2, cvb.Scalar.White)
        Next

        labels(2) = feat.labels(2)
        labels(3) = feat.labels(2)
    End Sub
End Class







Public Class Feature_Reduction : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Dim feat As New Feature_Stable
    Public Sub New()
        labels = {"", "", "Good features", "History of good features"}
        desc = "Get the features in a reduction grayscale image."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        reduction.Run(src)
        dst2 = src

        feat.Run(reduction.dst2)
        If task.heartBeat Then dst3.SetTo(0)
        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.White)
            DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.White)
        Next
    End Sub
End Class







Public Class Feature_MultiPass : Inherits TaskParent
    Dim feat As New Feature_Stable
    Public featurePoints As New List(Of cvb.Point2f)
    Dim sharpen As New PhotoShop_SharpenDetail
    Public Sub New()
        task.gOptions.RGBFilterActive.Checked = True
        task.gOptions.RGBFilterList.SelectedIndex = task.gOptions.RGBFilterList.Items.IndexOf("Filter_Laplacian")
        desc = "Run Feature_Stable twice and compare results."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(task.color)
        dst2 = src.Clone
        featurePoints = New List(Of cvb.Point2f)(task.features)
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
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next
        If task.heartBeat Then
            labels(2) = "Total features = " + CStr(featurePoints.Count) + ", pass counts = " + passCounts
        End If
    End Sub
End Class








Public Class Feature_PointTracker : Inherits TaskParent
    Dim flow As New Font_FlowText
    Public feat As New Feature_Stable
    Dim mPoints As New Match_Points
    Dim options As New Options_Features
    Public Sub New()
        flow.parentData = Me
        flow.dst = 3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        Dim correlationMin = options.correlationMin
        Dim templatePad = options.templatePad
        Dim templateSize = options.templateSize

        strOut = ""
        If mPoints.ptx.Count <= 3 Then
            mPoints.ptx.Clear()
            feat.Run(src)
            For Each pt In task.features
                mPoints.ptx.Add(pt)
                Dim rect = ValidateRect(New cvb.Rect(pt.X - templatePad, pt.Y - templatePad, templateSize, templateSize))
            Next
            strOut = "Restart tracking -----------------------------------------------------------------------------" + vbCrLf
        End If
        mPoints.Run(src)

        dst2 = src.Clone
        For i = mPoints.ptx.Count - 1 To 0 Step -1
            If mPoints.correlation(i) > correlationMin Then
                DrawCircle(dst2, mPoints.ptx(i), task.DotSize, task.HighlightColor)
                strOut += Format(mPoints.correlation(i), fmt3) + ", "
            Else
                mPoints.ptx.RemoveAt(i)
            End If
        Next
        If standaloneTest() Then
            flow.nextMsg = strOut
            flow.Run(empty)
        End If

        labels(2) = "Of the " + CStr(task.features.Count) + " input points, " + CStr(mPoints.ptx.Count) +
                    " points were tracked with correlation above " + Format(correlationMin, fmt2)
    End Sub
End Class








Public Class Feature_Delaunay : Inherits TaskParent
    Dim facet As New Delaunay_Contours
    Dim feat As New Feature_Stable
    Public Sub New()
        FindSlider("Min Distance to next").Value = 10
        desc = "Divide the image into contours with Delaunay using features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
            DrawCircle(dst3, pt, task.DotSize, cvb.Scalar.White)
        Next
        labels(3) = "There were " + CStr(task.features.Count) + " Delaunay contours"
    End Sub
End Class







Public Class Feature_LucasKanade : Inherits TaskParent
    Dim pyr As New FeatureFlow_LucasKanade
    Public ptList As New List(Of cvb.Point)
    Public ptLast As New List(Of cvb.Point)
    Dim ptHist As New List(Of List(Of cvb.Point))
    Public Sub New()
        desc = "Provide a trace of the tracked features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        pyr.Run(src)
        dst2 = src
        labels(2) = pyr.labels(2)

        If task.heartBeat Then dst3.SetTo(0)

        ptList.Clear()
        Dim stationary As Integer, motion As Integer
        For i = 0 To pyr.features.Count - 1
            Dim pt = New cvb.Point(pyr.features(i).X, pyr.features(i).Y)
            ptList.Add(pt)
            If ptLast.Contains(pt) Then
                DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
                stationary += 1
            Else
                DrawLine(dst3, pyr.lastFeatures(i), pyr.features(i), cvb.Scalar.White)
                motion += 1
            End If
        Next

        If task.heartBeat Then labels(3) = CStr(stationary) + " features were stationary and " + CStr(motion) + " features had some motion."
        ptLast = New List(Of cvb.Point)(ptList)
    End Sub
End Class







Public Class Feature_NearestCell : Inherits TaskParent
    Dim redC As New RedCloud_Basics
    Dim feat As New FeatureLeftRight_Basics
    Dim knn As New KNN_Basics
    Public Sub New()
        desc = "Find the nearest feature to every cell in task.redCells"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
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
            knn.trainInput.Add(New cvb.Point2f(mp.p1.X, mp.p1.Y))
        Next

        knn.Run(Nothing)

        For i = 0 To task.redCells.Count - 1
            Dim rc = task.redCells(i)
            rc.nearestFeature = knn.trainInput(knn.result(i, 0))
            DrawLine(dst3, rc.nearestFeature, rc.maxDStable, task.HighlightColor)
        Next
    End Sub
End Class








Public Class Feature_Points : Inherits TaskParent
    Public feat As New Feature_Stable
    Public Sub New()
        labels(3) = "Features found in the image"
        desc = "Use the sorted list of Delaunay regions to find the top X points to track."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        If task.heartBeat Then dst3.SetTo(0)

        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
        Next
        labels(2) = CStr(task.features.Count) + " targets were present with " + CStr(feat.options.featurePoints) + " requested."
    End Sub
End Class






Public Class Feature_Trace : Inherits TaskParent
    Dim track As New RedTrack_Features
    Public Sub New()
        desc = "Placeholder to help find RedTrack_Features"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        track.Run(src)
        dst2 = track.dst2
        labels = track.labels
    End Sub
End Class





Public Class Feature_TraceDelaunay : Inherits TaskParent
    Dim features As New Feature_Delaunay
    Public goodList As New List(Of List(Of cvb.Point2f)) ' stable points only
    Public Sub New()
        labels = {"Stable points highlighted", "", "", "Delaunay map of regions defined by the feature points"}
        desc = "Trace the GoodFeatures points using only Delaunay - no KNN or RedCloud or Matching."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        features.Run(src)
        dst3 = features.dst2

        If task.optionsChanged Then goodList.Clear()

        Dim ptList As New List(Of cvb.Point2f)(task.features)
        goodList.Add(ptList)

        If goodList.Count >= task.frameHistoryCount Then goodList.RemoveAt(0)

        dst2.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                DrawCircle(task.color, pt, task.DotSize, task.HighlightColor)
                Dim c = dst3.Get(Of cvb.Vec3b)(pt.Y, pt.X)
                DrawCircle(dst2, pt, task.DotSize + 1, c)
            Next
        Next
        labels(2) = CStr(task.features.Count) + " features were identified in the image."
    End Sub
End Class






Public Class Feature_ShiTomasi : Inherits TaskParent
    Dim harris As New Corners_HarrisDetector_CPP_VB
    Dim shiTomasi As New Corners_ShiTomasi_CPP_VB
    Dim options As New Options_ShiTomasi
    Public Sub New()
        FindSlider("Corner normalize threshold").Value = 15
        labels = {"", "", "Features in the left camera image", "Features in the right camera image"}
        desc = "Identify feature points in the left And right views"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()

        If options.useShiTomasi Then
            dst2 = task.leftView
            dst3 = task.rightView
            shiTomasi.Run(task.leftView)
            dst2.SetTo(cvb.Scalar.White, shiTomasi.dst3.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))

            shiTomasi.Run(task.rightView)
            dst3.SetTo(task.HighlightColor, shiTomasi.dst3.CvtColor(cvb.ColorConversionCodes.BGR2GRAY))
        Else
            harris.Run(task.leftView)
            dst2 = harris.dst2.Clone
            harris.Run(task.rightView)
            dst3 = harris.dst2
        End If
    End Sub
End Class






Public Class Feature_Generations : Inherits TaskParent
    Dim feat As New Feature_Stable
    Dim features As New List(Of cvb.Point)
    Dim gens As New List(Of Integer)
    Public Sub New()
        UpdateAdvice(traceName + ": Local options will determine how many features are present.")
        desc = "Find feature age maximum and average."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)

        Dim newfeatures As New SortedList(Of Integer, cvb.Point)(New compareAllowIdenticalIntegerInverted)
        For Each pt In task.featurePoints
            Dim index = features.IndexOf(pt)
            If index >= 0 Then newfeatures.Add(gens(index) + 1, pt) Else newfeatures.Add(1, pt)
        Next

        If task.heartBeat Then
            features.Clear()
            gens.Clear()
        End If

        features = New List(Of cvb.Point)(newfeatures.Values)
        gens = New List(Of Integer)(newfeatures.Keys)

        dst2 = src
        For i = 0 To features.Count - 1
            If gens(i) = 1 Then Exit For
            Dim pt = features(i)
            DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.White)
        Next

        If task.heartBeat Then
            labels(2) = CStr(features.Count) + " features found with max/average " + CStr(gens(0)) + "/" + Format(gens.Average, fmt0) + " generations"
        End If
    End Sub
End Class




' https://docs.opencvb.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_History : Inherits TaskParent
    Public features As New List(Of cvb.Point)
    Public feat As New Feature_Stable
    Dim featureHistory As New List(Of List(Of cvb.Point))
    Dim gens As New List(Of Integer)
    Public Sub New()
        desc = "Find good features across multiple frames."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim histCount = task.gOptions.FrameHistory.Value

        feat.Run(src)
        dst2 = src.Clone

        featureHistory.Add(New List(Of cvb.Point)(task.featurePoints))

        Dim newFeatures As New List(Of cvb.Point)
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
                    DrawCircle(dst2, pt, task.DotSize + 2, cvb.Scalar.Red)
                Else
                    whiteCount += 1
                    DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
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






Public Class Feature_GridPopulation : Inherits TaskParent
    Dim feat As New Feature_Stable
    Public Sub New()
        dst3 = New cvb.Mat(dst3.Size(), cvb.MatType.CV_8U, cvb.Scalar.All(0))
        labels(3) = "Click 'Show grid mask overlay' to see grid boundaries."
        desc = "Find the feature population for each cell."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        feat.Run(src)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

        dst3.SetTo(0)
        For Each pt In task.featurePoints
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next

        For Each roi In task.gridRects
            Dim test = dst3(roi).FindNonZero()
            SetTrueText(CStr(test.Rows), roi.TopLeft, 3)
        Next
    End Sub
End Class










Public Class Feature_Compare : Inherits TaskParent
    Dim feat As New Feature_Stable
    Dim noFrill As New Feature_Basics
    Dim saveLFeatures As New List(Of cvb.Point2f)
    Dim saveRFeatures As New List(Of cvb.Point2f)
    Public Sub New()
        desc = "Prepare features for the left and right views"
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        task.features = New List(Of cvb.Point2f)(saveLFeatures)
        feat.Run(src.Clone)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)
        saveLFeatures = New List(Of cvb.Point2f)(task.features)

        task.features = New List(Of cvb.Point2f)(saveRFeatures)
        noFrill.Run(src.Clone)
        dst3 = noFrill.dst2
        labels(3) = "With no correlation coefficients " + noFrill.labels(2)
        saveRFeatures = New List(Of cvb.Point2f)(task.features)
    End Sub
End Class






Public Class Feature_Gather : Inherits TaskParent
    Dim harris As New Corners_HarrisDetector_CPP_VB
    Dim FAST As New Corners_Basics
    Dim myOptions As New Options_FeatureGather
    Public features As New List(Of cvb.Point2f)
    Public ptList As New List(Of cvb.Point)
    Dim brisk As New BRISK_Basics
    Public options As New Options_Features
    Public Sub New()
        FindSlider("Feature Sample Size").Value = 400
        FindSlider("Min Distance to next").Value = 10
        cPtr = Agast_Open()
        desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        options.RunOpt()
        myOptions.RunOpt()

        If src.Channels() <> 1 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2GRAY)

        Select Case myOptions.featureSource
            Case FeatureSrc.GoodFeaturesFull
                features = cvb.Cv2.GoodFeaturesToTrack(src, options.featurePoints, options.quality, options.minDistance, New cvb.Mat,
                                                      options.blockSize, True, options.k).ToList
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.GoodFeaturesGrid
                options.featurePoints = 4
                features.Clear()
                For i = 0 To task.gridRects.Count - 1
                    Dim roi = task.gridRects(i)
                    Dim tmpFeatures = cvb.Cv2.GoodFeaturesToTrack(src(roi), options.featurePoints, options.quality, options.minDistance, New cvb.Mat,
                                                                 options.blockSize, True, options.k).ToList
                    For j = 0 To tmpFeatures.Count - 1
                        features.Add(New cvb.Point2f(tmpFeatures(j).X + roi.X, tmpFeatures(j).Y + roi.Y))
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

                Dim ptMat = cvb.Mat.FromPixelData(Agast_Count(cPtr), 1, cvb.MatType.CV_32FC2, imagePtr).Clone
                features.Clear()
                If standaloneTest() Then dst2 = src

                For i = 0 To ptMat.Rows - 1
                    Dim pt = ptMat.Get(Of cvb.Point2f)(i, 0)
                    features.Add(pt)
                    If standaloneTest() Then DrawCircle(dst2, pt, task.DotSize, cvb.Scalar.White)
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
            ptList.Add(New cvb.Point(pt.X, pt.Y))
        Next
        If standaloneTest() Then
            dst2 = task.color.Clone
            For Each pt In features
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            Next
        End If
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Agast_Close(cPtr)
    End Sub
End Class




Public Class Feature_Agast : Inherits TaskParent
    Dim stablePoints As List(Of Point2f)
    Dim agastFD As AgastFeatureDetector
    Dim lastPoints As List(Of Point2f)
    Public Sub New()
        agastFD = AgastFeatureDetector.Create(10, True, AgastFeatureDetector.DetectorType.OAST_9_16)
        desc = "Use the Agast Feature Detector in the OpenCV Contrib."
        stablePoints = New List(Of Point2f)()
        lastPoints = New List(Of Point2f)()
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        Dim resizeFactor As Integer = 1
        Dim input As New Mat()
        If src.Cols >= 1280 Then
            Cv2.Resize(src, input, New cvb.Size(src.Cols \ 4, src.Rows \ 4))
            resizeFactor = 4
        Else
            input = src
        End If
        Dim keypoints As KeyPoint() = agastFD.Detect(input)
        If task.heartBeat OrElse lastPoints.Count < 10 Then
            lastPoints.Clear()
            For Each kpt As KeyPoint In keypoints
                lastPoints.Add(New Point2f(CSng(Math.Round(kpt.Pt.X)) * resizeFactor, CSng(Math.Round(kpt.Pt.Y)) * resizeFactor))
            Next
        End If
        stablePoints.Clear()
        dst2 = src.Clone()
        For Each pt As KeyPoint In keypoints
            Dim p1 As New Point2f(CSng(Math.Round(pt.Pt.X * resizeFactor)), CSng(Math.Round(pt.Pt.Y * resizeFactor)))
            If lastPoints.Contains(p1) Then
                stablePoints.Add(p1)
                DrawCircle(dst2, p1, task.DotSize, New Scalar(0, 0, 255))
            End If
        Next
        lastPoints = New List(Of Point2f)(stablePoints)
        If task.midHeartBeat Then
            labels(2) = $"{keypoints.Length} features found and {stablePoints.Count} of them were stable"
        End If
        labels(2) = $"Found {keypoints.Length} features"
    End Sub
End Class









Public Class Feature_AKaze : Inherits TaskParent
    Dim kazeKeyPoints As KeyPoint() = Nothing
    Public Sub New()
        labels(2) = "AKAZE key points"
        desc = "Find keypoints using AKAZE algorithm."
    End Sub
    Public Sub RunAlg(src As cvb.Mat)
        dst2 = src.Clone()
        If src.Channels() <> 1 Then
            src = src.CvtColor(ColorConversionCodes.BGR2GRAY)
        End If
        Dim kaze = AKAZE.Create()
        Dim kazeDescriptors As New Mat()
        kaze.DetectAndCompute(src, Nothing, kazeKeyPoints, kazeDescriptors)
        For i As Integer = 0 To kazeKeyPoints.Length - 1
            DrawCircle(dst2, kazeKeyPoints(i).Pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class
