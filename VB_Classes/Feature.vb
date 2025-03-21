Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports VB_Classes.OptionParent
Public Class Feature_Basics : Inherits TaskParent
    Public options As New Options_Features
    Dim method As New Feature_Methods
    Public Sub New()
        UpdateAdvice(traceName + ": Use 'Options_Features' to control output.")
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find good features to track in a BGR image using the motion mask+"
    End Sub
    Public Function motionFilter(featureInput As List(Of cv.Point2f)) As List(Of cv.Point2f)
        Static ptList = New List(Of cv.Point2f)(featureInput)

        Dim newSet As New List(Of cv.Point2f)
        For Each pt In ptList
            Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val = 0 Then newSet.Add(pt)
        Next

        For Each pt In featureInput
            Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val <> 0 Then newSet.Add(pt)
        Next

        Dim ptSort As New SortedList(Of Integer, cv.Point2f)(New compareAllowIdenticalInteger)
        ' Not technically needed but organizes all the points from top to bottom, left to right.
        For Each pt In newSet
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            ptSort.Add(index, pt)
        Next

        ptList = New List(Of cv.Point2f)(ptSort.Values)
        Return ptList
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        dst2 = src.Clone

        method.Run(src)

        Dim ptlist = motionFilter(method.features)

        task.features.Clear()
        task.featurePoints.Clear()
        For Each pt In ptlist
            task.features.Add(pt)
            task.featurePoints.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
        Next

        dst3.SetTo(0)
        For i = 0 To ptlist.Count - 1
            Dim pt = ptlist(i)
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next

        labels(2) = method.labels(2)
    End Sub
End Class






Public Class Feature_Methods : Inherits TaskParent
    Dim harris As Corners_HarrisDetector_CPP
    Dim FAST As Corners_Basics
    Dim featureMethod As New Options_FeatureGather
    Public features As New List(Of cv.Point2f)
    Public featurePoints As New List(Of cv.Point)
    Public ptList As New List(Of cv.Point)
    Dim brisk As BRISK_Basics
    Public options As New Options_Features
    Dim methodList As New List(Of Integer)({})
    Public Sub New()
        desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        featureMethod.RunOpt()
        Static frm = optiBase.FindFrm("Options_FeatureGather Radio Buttons")
        Dim featureSource As Integer
        For i = 0 To frm.check.Count - 1
            If frm.check(i).Checked Then
                featureSource = Choose(i + 1, FeatureSrc.GoodFeaturesFull, FeatureSrc.GoodFeaturesGrid,
                                       FeatureSrc.Agast, FeatureSrc.BRISK, FeatureSrc.Harris,
                                       FeatureSrc.FAST)
                Exit For
            End If
        Next
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Select Case featureSource
            Case FeatureSrc.GoodFeaturesFull
                features = cv.Cv2.GoodFeaturesToTrack(src, options.featurePoints, options.quality,
                                                       options.minDistance, New cv.Mat,
                                                       options.blockSize, True, options.k).ToList
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.GoodFeaturesGrid
                options.featurePoints = 4
                features.Clear()
                For i = 0 To task.gridRects.Count - 1
                    Dim roi = task.gridRects(i)
                    Dim tmpFeatures = cv.Cv2.GoodFeaturesToTrack(src(roi), options.featurePoints, options.quality, options.minDistance, New cv.Mat,
                                                                 options.blockSize, True, options.k).ToList
                    For j = 0 To tmpFeatures.Count - 1
                        features.Add(New cv.Point2f(tmpFeatures(j).X + roi.X, tmpFeatures(j).Y + roi.Y))
                    Next
                Next
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.Agast
                If cPtr = 0 Then cPtr = Agast_Open()
                src = task.color.Clone
                Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
                Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)

                Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
                Dim imagePtr = Agast_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.agastThreshold)
                handleSrc.Free()

                Dim ptMat = cv.Mat.FromPixelData(Agast_Count(cPtr), 1, cv.MatType.CV_32FC2, imagePtr).Clone
                features.Clear()
                If standaloneTest() Then dst2 = src

                For i = 0 To ptMat.Rows - 1
                    Dim pt = ptMat.Get(Of cv.Point2f)(i, 0)
                    features.Add(pt)
                    If standaloneTest() Then DrawCircle(dst2, pt, task.DotSize, white)
                Next

                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.BRISK
                If brisk Is Nothing Then brisk = New BRISK_Basics
                brisk.Run(src)
                features = brisk.features
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.Harris
                If harris Is Nothing Then harris = New Corners_HarrisDetector_CPP
                harris.Run(src)
                features = harris.features
                labels(2) = "Harris Detector produced " + CStr(features.Count) + " features"
            Case FeatureSrc.FAST
                If FAST Is Nothing Then FAST = New Corners_Basics
                FAST.Run(src)
                features = FAST.features
                labels(2) = "FAST produced " + CStr(features.Count) + " features"
        End Select

        featurePoints.Clear()
        For Each pt In features
            featurePoints.Add(New cv.Point(pt.X, pt.Y))
        Next

        ptList.Clear()
        For Each pt In features
            ptList.Add(New cv.Point(pt.X, pt.Y))
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





' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_NoMotionTest : Inherits TaskParent
    Public options As New Options_Features
    Dim method As New Feature_Methods
    Public Sub New()
        UpdateAdvice(traceName + ": Use 'Options_Features' to control output.")
        desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent results."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        dst2 = src.Clone

        method.Run(src)

        task.features.Clear()
        task.featurePoints.Clear()
        For Each pt In method.features
            task.features.Add(pt)
            task.featurePoints.Add(New cv.Point(pt.X, pt.X))
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next

        labels(2) = method.labels(2)
    End Sub
End Class








' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_KNN : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public featurePoints As New List(Of cv.Point2f)
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find good features to track in a BGR image but use the same point if closer than a threshold"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runFeature(src)

        knn.queries = New List(Of cv.Point2f)(task.features)
        If task.firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(src)

        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = task.features(i)
            If pt.DistanceTo(qPt) > task.feat.options.minDistance Then knn.trainInput(trainIndex) = task.features(i)
        Next
        featurePoints = New List(Of cv.Point2f)(knn.trainInput)

        src.CopyTo(dst2)
        dst3.SetTo(0)
        For Each pt In featurePoints
            DrawCircle(dst2, pt, task.DotSize + 2, white)
            DrawCircle(dst3, pt, task.DotSize + 2, white)
        Next

        labels(2) = task.feat.labels(2)
        labels(3) = task.feat.labels(2)
    End Sub
End Class







Public Class Feature_Reduction : Inherits TaskParent
    Dim reduction As New Reduction_Basics
    Public Sub New()
        labels = {"", "", "Good features", "History of good features"}
        desc = "Get the features in a reduction grayscale image."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        reduction.Run(src)
        dst2 = src

        runFeature(reduction.dst2)
        If task.heartBeat Then dst3.SetTo(0)
        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, white)
            DrawCircle(dst3, pt, task.DotSize, white)
        Next
    End Sub
End Class







Public Class Feature_PointTracker : Inherits TaskParent
    Dim flow As New Font_FlowText
    Dim mPoints As New Match_Points
    Dim options As New Options_Features
    Public Sub New()
        flow.parentData = Me
        flow.dst = 3
        labels(3) = "Correlation coefficients for each remaining cell"
        desc = "Use the top X goodFeatures and then use matchTemplate to find track them."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        Dim correlationMin = options.correlationMin
        Dim templatePad = options.templatePad
        Dim templateSize = options.templateSize

        strOut = ""
        If mPoints.ptx.Count <= 3 Then
            mPoints.ptx.Clear()
            For Each pt In task.features
                mPoints.ptx.Add(pt)
                Dim rect = ValidateRect(New cv.Rect(pt.X - templatePad, pt.Y - templatePad, templateSize, templateSize))
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
            flow.Run(src)
        End If

        labels(2) = "Of the " + CStr(task.features.Count) + " input points, " + CStr(mPoints.ptx.Count) +
                    " points were tracked with correlation above " + Format(correlationMin, fmt2)
    End Sub
End Class








Public Class Feature_Delaunay : Inherits TaskParent
    Dim delaunay As New Delaunay_Contours
    Dim options As New Options_Features
    Public Sub New()
        optiBase.FindSlider("Min Distance to next").Value = 10
        desc = "Divide the image into contours with Delaunay using features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        dst2 = runFeature(src)
        labels(2) = task.feat.labels(2)

        delaunay.Run(src)
        dst3 = delaunay.dst2
        For Each pt In task.features
            DrawCircle(dst3, pt, task.DotSize, white)
        Next
        labels(3) = "There were " + CStr(task.features.Count) + " Delaunay contours"
    End Sub
End Class







Public Class Feature_LucasKanade : Inherits TaskParent
    Dim pyr As New FeatureFlow_LucasKanade
    Public ptList As New List(Of cv.Point)
    Public ptLast As New List(Of cv.Point)
    Dim ptHist As New List(Of List(Of cv.Point))
    Public Sub New()
        desc = "Provide a trace of the tracked features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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
                DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
                stationary += 1
            Else
                DrawLine(dst3, pyr.lastFeatures(i), pyr.features(i), white)
                motion += 1
            End If
        Next

        If task.heartBeat Then labels(3) = CStr(stationary) + " features were stationary and " + CStr(motion) + " features had some motion."
        ptLast = New List(Of cv.Point)(ptList)
    End Sub
End Class









Public Class Feature_Points : Inherits TaskParent
    Public Sub New()
        labels(3) = "Features found in the image"
        desc = "Use the sorted list of Delaunay regions to find the top X points to track."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runFeature(src)
        If task.heartBeat Then dst3.SetTo(0)

        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
        Next
        labels(2) = CStr(task.features.Count) + " targets were present with " + CStr(task.feat.options.featurePoints) + " requested."
    End Sub
End Class






Public Class Feature_Trace : Inherits TaskParent
    Dim track As New RedTrack_Features
    Public Sub New()
        desc = "Placeholder to help find RedTrack_Features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        track.Run(src)
        dst2 = track.dst2
        labels = track.labels
    End Sub
End Class





Public Class Feature_TraceDelaunay : Inherits TaskParent
    Dim features As New Feature_Delaunay
    Public goodList As New List(Of List(Of cv.Point2f)) ' stable points only
    Public Sub New()
        labels = {"Stable points highlighted", "", "", "Delaunay map of regions defined by the feature points"}
        desc = "Trace the GoodFeatures points using only Delaunay - no KNN or RedCloud or Matching."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        features.Run(src)
        dst3 = features.dst2

        If task.optionsChanged Then goodList.Clear()

        Dim ptList As New List(Of cv.Point2f)(task.features)
        goodList.Add(ptList)

        If goodList.Count >= task.frameHistoryCount Then goodList.RemoveAt(0)

        dst2.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                DrawCircle(task.color, pt, task.DotSize, task.HighlightColor)
                Dim c = dst3.Get(Of cv.Vec3b)(pt.Y, pt.X)
                DrawCircle(dst2, pt, task.DotSize + 1, c)
            Next
        Next
        labels(2) = CStr(task.features.Count) + " features were identified in the image."
    End Sub
End Class






Public Class Feature_ShiTomasi : Inherits TaskParent
    Dim harris As New Corners_HarrisDetector_CPP
    Dim shiTomasi As New Corners_ShiTomasi_CPP
    Dim options As New Options_ShiTomasi
    Public Sub New()
        optiBase.FindSlider("Corner normalize threshold").Value = 15
        labels = {"", "", "Features in the left camera image", "Features in the right camera image"}
        desc = "Identify feature points in the left And right views"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If options.useShiTomasi Then
            dst2 = task.leftView
            dst3 = task.rightView
            shiTomasi.Run(task.leftView)
            dst2.SetTo(cv.Scalar.White, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

            shiTomasi.Run(task.rightView)
            dst3.SetTo(task.HighlightColor, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Else
            harris.Run(task.leftView)
            dst2 = harris.dst2.Clone
            harris.Run(task.rightView)
            dst3 = harris.dst2
        End If
    End Sub
End Class






Public Class Feature_Generations : Inherits TaskParent
    Dim features As New List(Of cv.Point)
    Dim gens As New List(Of Integer)
    Public Sub New()
        UpdateAdvice(traceName + ": Local options will determine how many features are present.")
        desc = "Find feature age maximum and average."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        runFeature(src)

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
            DrawCircle(dst2, pt, task.DotSize, white)
        Next

        If task.heartBeat And gens.Count > 0 Then
            labels(2) = CStr(features.Count) + " features found with max/average " + CStr(gens(0)) + "/" + Format(gens.Average, fmt0) + " generations"
        End If
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_History : Inherits TaskParent
    Public features As New List(Of cv.Point)
    Dim featureHistory As New List(Of List(Of cv.Point))
    Dim gens As New List(Of Integer)
    Public Sub New()
        desc = "Find good features across multiple frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
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

        Dim threshold = If(task.frameHistoryCount = 1, 0, 1)
        features.Clear()
        Dim whiteCount As Integer
        For i = 0 To newFeatures.Count - 1
            If gens(i) > threshold Then
                Dim pt = newFeatures(i)
                features.Add(pt)
                If gens(i) < task.frameHistoryCount Then
                    DrawCircle(dst2, pt, task.DotSize + 2, cv.Scalar.Red)
                Else
                    whiteCount += 1
                    DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
                End If
            End If
        Next

        If featureHistory.Count > task.frameHistoryCount Then featureHistory.RemoveAt(0)
        If task.heartBeat Then
            labels(2) = CStr(features.Count) + "/" + CStr(whiteCount) + " present/present on every frame" +
                        " Red is a recent addition, yellow is present on previous " +
                        CStr(task.frameHistoryCount) + " frames"
        End If
    End Sub
End Class






Public Class Feature_GridPopulation : Inherits TaskParent
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(3) = "Click 'Show grid mask overlay' to see grid boundaries."
        desc = "Find the feature population for each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runFeature(src)

        labels(2) = task.feat.labels(2)

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









Public Class Feature_AKaze : Inherits TaskParent
    Dim kazeKeyPoints As cv.KeyPoint() = Nothing
    Public Sub New()
        labels(2) = "AKAZE key points"
        desc = "Find keypoints using AKAZE algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone()
        If src.Channels() <> 1 Then
            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        Dim kaze = cv.AKAZE.Create()
        Dim kazeDescriptors As New cv.Mat()
        kaze.DetectAndCompute(src, Nothing, kazeKeyPoints, kazeDescriptors)
        For i As Integer = 0 To kazeKeyPoints.Length - 1
            DrawCircle(dst2, kazeKeyPoints(i).Pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class





Public Class Feature_RedCloud : Inherits TaskParent
    Public Sub New()
        desc = "Show the feature points in the RedCloud output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runRedC(src, labels(2))

        For Each pt In task.featurePoints
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next
    End Sub
End Class






Public Class Feature_WithDepth : Inherits TaskParent
    Public Sub New()
        desc = "Show the feature points that have depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = runFeature(src)

        dst3 = src
        Dim depthCount As Integer
        For Each pt In task.featurePoints
            Dim val = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
            If val > 0 Then
                DrawCircle(dst3, pt, task.DotSize, task.HighlightColor)
                depthCount += 1
            End If
        Next
        labels(3) = CStr(depthCount) + " features had depth or " +
                    Format(depthCount / task.features.Count, "0%")
    End Sub
End Class





Public Class Feature_Matching : Inherits TaskParent
    Public features As New List(Of cv.Point)
    Public motionPoints As New List(Of cv.Point)
    Dim match As New Match_Basics
    Dim method As New Feature_Methods
    Dim options As New Options_MatchCorrelation
    Public Sub New()
        optiBase.FindSlider("Feature Sample Size").Value = 150
        desc = "Use correlation coefficient to keep features from frame to frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        Static fpLastSrc = src.Clone

        Dim matched As New List(Of cv.Point)
        motionPoints.Clear()
        For Each pt In features
            Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val = 0 Then
                Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim r = task.gridRects(index)
                match.template = fpLastSrc(r)
                match.Run(src(r))
                If match.correlation > options.MinCorrelation Then matched.Add(pt)
            Else
                motionPoints.Add(pt)
            End If
        Next

        labels(2) = "There were " + CStr(features.Count) + " features identified and " + CStr(matched.Count) +
                    " were matched to the previous frame"

        If matched.Count < match.options.featurePoints / 2 Then
            method.Run(src)
            features = method.featurePoints
        Else
            features = New List(Of cv.Point)(matched)
        End If

        dst2 = src.Clone
        For Each pt In features
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next

        fpLastSrc = src.Clone
    End Sub
End Class





'Public Class Feature_Stable : Inherits TaskParent
'    Dim nextMatList As New List(Of cv.Mat)
'    Dim ptList As New List(Of cv.Point2f)
'    Dim knn As New KNN_Basics
'    Dim ptLost As New List(Of cv.Point2f)
'    Dim method As New Feature_Methods
'    Dim featureMatList As New List(Of cv.Mat)
'    Public options As New Options_Features
'    Dim noMotionFrames As Single
'    Public Sub New()
'        task.features.Clear() ' in case it was previously in use...
'        desc = "Identify features with GoodFeaturesToTrack but manage them with MatchTemplate"
'    End Sub
'    Public Overrides Sub RunAlg(src As cv.Mat)
'        options.RunOpt()
'        dst2 = src.Clone
'        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

'        method.Run(src)

'        If task.optionsChanged Then
'            task.features.Clear()
'            featureMatList.Clear()
'        End If

'        nextMatList.Clear()
'        ptList.Clear()
'        Dim correlationMat As New cv.Mat
'        Dim saveFeatureCount = Math.Min(featureMatList.Count, task.features.Count)
'        For i = 0 To saveFeatureCount - 1
'            Dim pt = task.features(i)
'            Dim rect = ValidateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, featureMatList(i).Width, featureMatList(i).Height))
'            If method.ptList.Contains(pt) = False Then
'                cv.Cv2.MatchTemplate(src(rect), featureMatList(i), correlationMat, cv.TemplateMatchModes.CCoeffNormed)
'                If correlationMat.Get(Of Single)(0, 0) < options.correlationMin Then
'                    Dim ptNew = New cv.Point2f(CInt(pt.X), CInt(pt.Y))
'                    If ptLost.Contains(ptNew) = False Then ptLost.Add(ptNew)
'                    Continue For
'                End If
'            End If
'            nextMatList.Add(featureMatList(i))
'            ptList.Add(pt)
'        Next

'        Dim survivorPercent As Single = (ptList.Count - ptLost.Count) / saveFeatureCount
'        Dim extra = 1 + (1 - options.resyncThreshold)
'        task.features = New List(Of cv.Point2f)(ptList)

'        If task.features.Count < method.features.Count * options.resyncThreshold Or task.features.Count > extra * method.features.Count Then
'            task.featureMotion = True
'            ptLost.Clear()
'            featureMatList.Clear()
'            task.features.Clear()
'            For Each pt In method.features
'                Dim rect = ValidateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad, options.templateSize, options.templateSize))
'                featureMatList.Add(src(rect))
'                task.features.Add(pt)
'            Next
'        Else
'            If ptLost.Count > 0 Then
'                knn.queries = ptLost
'                knn.trainInput = method.features
'                knn.Run(Nothing)

'                For i = 0 To knn.queries.Count - 1
'                    Dim pt = knn.queries(i)
'                    Dim rect = ValidateRect(New cv.Rect(pt.X - options.templatePad, pt.Y - options.templatePad,
'                                                         options.templateSize, options.templateSize))
'                    featureMatList.Add(src(rect))
'                    task.features.Add(knn.trainInput(knn.result(i, 0)))
'                Next
'            End If
'            task.featureMotion = False
'            noMotionFrames += 1
'            featureMatList = New List(Of cv.Mat)(nextMatList)
'        End If

'        task.featurePoints.Clear()
'        For Each pt In task.features
'            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
'            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))
'        Next
'        If task.heartBeat Then
'            If task.featureMotion = True Then survivorPercent = 0
'            labels(2) = Format(survivorPercent, "0%") + " of " + CStr(task.features.Count) + " features were matched to the previous frame using correlation and " +
'                        CStr(ptLost.Count) + " features had to be relocated"
'        End If
'        If task.heartBeat Then
'            Dim percent = noMotionFrames / task.fpsRate
'            If percent > 1 Then percent = 1
'            labels(3) = CStr(noMotionFrames) + " frames since the last heartbeat with no motion " +
'                        " or " + Format(percent, "0%")
'            noMotionFrames = 0
'        End If
'    End Sub
'End Class





Public Class Feature_SteadyCam : Inherits TaskParent
    Public options As New Options_Features
    Public Sub New()
        optiBase.FindSlider("Threshold Percent for Resync").Value = 50
        desc = "Track features using correlation without the motion mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        Static features As New List(Of cv.Point)(task.featurePoints)
        Static lastSrc As cv.Mat = src.Clone

        Dim resync = features.Count / task.features.Count < options.resyncThreshold
        If task.heartBeat Or task.optionsChanged Or resync Then
            features = New List(Of cv.Point)(task.featurePoints)
        End If

        Dim ptList = New List(Of cv.Point)(features)
        Dim correlationMat As New cv.Mat
        Dim mode = cv.TemplateMatchModes.CCoeffNormed
        features.Clear()
        For Each pt In ptList
            Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim r = task.gridRects(index)
            cv.Cv2.MatchTemplate(src(r), lastSrc(r), correlationMat, mode)
            If correlationMat.Get(Of Single)(0, 0) >= options.correlationMin Then
                features.Add(pt)
            End If
        Next

        dst2 = src
        For Each pt In features
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next

        lastSrc = src.Clone
        labels(2) = CStr(features.Count) + " features were validated by the correlation coefficient"
    End Sub
End Class







Public Class Feature_FacetPoints : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Public Sub New()
        desc = "Assign each delaunay point to a RedCell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then runRedC(src)

        delaunay.inputPoints = task.features
        delaunay.Run(src)

        For Each pt In delaunay.ptList
            Dim index = task.rcMap.Get(Of Byte)(pt.Y, pt.X)
            If index = 0 Then Continue For
            Dim rc = task.rcList(index)
            Dim val = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
            If val <> 0 Then
                rc.ptFacets.Add(pt)
                task.rcList(index) = rc
            End If
        Next

        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)

        For Each rc In task.rcList
            For Each pt In rc.ptFacets
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            Next
        Next

        If standalone Then
            Dim rc = task.rcList(task.rc.index)
            task.color.Rectangle(rc.rect, task.HighlightColor, task.lineWidth)
            For Each pt In rc.ptFacets
                DrawCircle(task.color, pt, task.DotSize, task.HighlightColor)
            Next
        End If
    End Sub
End Class







Public Class Feature_GridPoints : Inherits TaskParent
    Public Sub New()
        desc = "Assign each corner of a grid rect to a RedCell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If standalone Then runRedC(src)

        For Each pt In task.gridPoints
            Dim index = task.rcMap.Get(Of Byte)(pt.Y, pt.X)
            If index = 0 Then Continue For
            Dim rc = task.rcList(index)
            Dim val = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
            If val <> 0 Then
                rc.ptList.Add(pt)
                task.rcList(index) = rc
            End If
        Next

        dst2 = task.redC.dst2
        labels(2) = task.redC.labels(2)

        If standalone Then
            Dim rc = task.rcList(task.rc.index)
            dst2.Rectangle(rc.rect, task.HighlightColor, task.lineWidth)
            For Each pt In rc.ptList
                DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            Next
        End If
    End Sub
End Class





Public Class Feature_NoMotion : Inherits TaskParent
    Public options As New Options_Features
    Dim method As New Feature_Methods
    Public featurePoints As New List(Of cv.Point)
    Public Sub New()
        UpdateAdvice(traceName + ": Use 'Options_Features' to control output.")
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find good features to track in a BGR image using the motion mask+"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()
        dst2 = src.Clone

        method.Run(src)

        featurePoints = New List(Of cv.Point)(method.featurePoints)

        dst3.SetTo(0)
        For Each pt In featurePoints
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next

        labels(2) = method.labels(2)
    End Sub
End Class





Public Class Feature_AgastHeartbeat : Inherits TaskParent
    Dim stablePoints As List(Of cv.Point2f)
    Dim agastFD As cv.AgastFeatureDetector
    Dim lastPoints As List(Of cv.Point2f)
    Public Sub New()
        agastFD = cv.AgastFeatureDetector.Create(10, True, cv.AgastFeatureDetector.DetectorType.OAST_9_16)
        desc = "Use the Agast Feature Detector in the OpenCV Contrib."
        stablePoints = New List(Of cv.Point2f)()
        lastPoints = New List(Of cv.Point2f)()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim resizeFactor As Integer = 1
        Dim input As New cv.Mat()
        If src.Cols >= 1280 Then
            cv.Cv2.Resize(src, input, New cv.Size(src.Cols \ 4, src.Rows \ 4))
            resizeFactor = 4
        Else
            input = src
        End If
        Dim keypoints As cv.KeyPoint() = agastFD.Detect(input)
        If task.heartBeat OrElse lastPoints.Count < 10 Then
            lastPoints.Clear()
            For Each kpt As cv.KeyPoint In keypoints
                lastPoints.Add(kpt.Pt)
            Next
        End If
        stablePoints.Clear()
        dst2 = src.Clone()
        For Each pt As cv.KeyPoint In keypoints
            Dim p1 As New cv.Point2f(CSng(Math.Round(pt.Pt.X * resizeFactor)), CSng(Math.Round(pt.Pt.Y * resizeFactor)))
            If lastPoints.Contains(p1) Then
                stablePoints.Add(p1)
                DrawCircle(dst2, p1, task.DotSize, New cv.Scalar(0, 0, 255))
            End If
        Next
        lastPoints = New List(Of cv.Point2f)(stablePoints)
        If task.midHeartBeat Then
            labels(2) = $"{keypoints.Length} features found and {stablePoints.Count} of them were stable"
        End If
        labels(2) = $"Found {keypoints.Length} features"
    End Sub
End Class





Public Class Feature_Agast : Inherits TaskParent
    Dim agastFD As cv.AgastFeatureDetector
    Dim stablePoints As New List(Of cv.Point2f)
    Dim options As New Options_Agast
    Public Sub New()
        desc = "Use the Agast Feature Detector in the OpenCV Contrib."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.RunOpt()

        If task.optionsChanged Then
            If agastFD IsNot Nothing Then agastFD.Dispose()
            agastFD = cv.AgastFeatureDetector.Create(options.agastThreshold, options.useNonMaxSuppression,
                                                     cv.AgastFeatureDetector.DetectorType.OAST_9_16)
        End If

        Dim keypoints As cv.KeyPoint() = agastFD.Detect(src)

        Dim currPoints As New List(Of cv.Point2f)
        For Each kpt As cv.KeyPoint In keypoints
            currPoints.Add(kpt.Pt)
        Next

        Dim newList As New List(Of cv.Point2f)
        For Each pt In stablePoints
            Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val = 0 Then newList.Add(pt)
        Next

        For Each pt In currPoints
            Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val <> 0 Then newList.Add(pt)
        Next

        stablePoints = New List(Of cv.Point2f)(newList)
        dst2 = src
        For Each pt In stablePoints
            DrawCircle(dst2, pt, task.DotSize, task.HighlightColor)
        Next
        labels(2) = $"Found {keypoints.Length} features with agast"
    End Sub
End Class
