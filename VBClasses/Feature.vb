Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports VBClasses.OptionParent
Imports cv = OpenCvSharp
Public Class Feature_Basics : Inherits TaskParent
    Public options As New Options_Features
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        labels(3) = "CV_8U mask with all the features present."
        desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        dst2 = task.color.Clone

        Dim features As New List(Of cv.Point2f)
        Dim ptNew As New List(Of cv.Point2f)
        If task.optionsChanged = False Then
            For Each pt In task.features
                Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 0 Then ptNew.Add(pt)
            Next
        End If

        Select Case task.featureSource
            Case FeatureSrc.GoodFeaturesFull
                features = cv.Cv2.GoodFeaturesToTrack(task.gray, task.FeatureSampleSize, options.quality,
                                                      task.minDistance, New cv.Mat,
                                                      options.blockSize, True, options.k).ToList
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.GoodFeaturesGrid
                task.FeatureSampleSize = 4
                features.Clear()
                For i = 0 To task.gridRects.Count - 1
                    Dim roi = task.gridRects(i)
                    Dim tmpFeatures = cv.Cv2.GoodFeaturesToTrack(task.gray(roi), task.FeatureSampleSize, options.quality,
                                                                 task.minDistance, New cv.Mat, options.blockSize,
                                                                 True, options.k).ToList
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

                For i = 0 To ptMat.Rows - 1
                    Dim pt = ptMat.Get(Of cv.Point2f)(i, 0)
                    features.Add(pt)
                    If standaloneTest() Then DrawCircle(dst2, pt, task.DotSize, white)
                Next

                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.BRISK
                Static brisk As New BRISK_Basics
                brisk.Run(task.gray)
                features = brisk.features
                labels(2) = "GoodFeatures produced " + CStr(features.Count) + " features"
            Case FeatureSrc.Harris
                Static harris As New Corners_HarrisDetector_CPP
                harris.Run(task.gray)
                features = harris.features
                labels(2) = "Harris Detector produced " + CStr(features.Count) + " features"
            Case FeatureSrc.FAST
                Static FAST As New Corners_Basics
                FAST.Run(task.gray)
                features = task.features
                labels(2) = "FAST produced " + CStr(features.Count) + " features"
            Case FeatureSrc.LineInput
                task.logicalLines.Clear()
                For Each lp In task.lines.lpList
                    features.Add(lp.p1)
                    features.Add(lp.p2)
                    task.logicalLines.Add(lp)
                Next
        End Select

        task.fpFromGridCellLast = New List(Of Integer)(task.fpFromGridCell)
        task.fpLastList = New List(Of fpData)(task.fpList)

        If task.optionsChanged Or ptNew.Count = 0 Then
            For Each pt In features
                ptNew.Add(pt)
            Next
        Else
            For Each pt In features
                Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 255 Then ptNew.Add(pt)
            Next
        End If

        Dim sortByGrid As New SortedList(Of Single, cv.Point2f)(New compareAllowIdenticalSingle)
        For Each pt In ptNew
            Dim index = task.grid.gridMap.Get(Of Integer)(pt.Y, pt.X)
            sortByGrid.Add(index, pt)
        Next

        task.features.Clear()
        task.featurePoints.Clear()
        task.fpFromGridCell.Clear()
        For i = 0 To sortByGrid.Count - 1
            Dim pt = sortByGrid.ElementAt(i).Value
            task.features.Add(pt)
            task.featurePoints.Add(New cv.Point(pt.X, pt.Y))

            Dim nextIndex = task.grid.gridMap.Get(Of Single)(pt.Y, pt.X)
            task.fpFromGridCell.Add(nextIndex)
        Next


        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next

        labels(2) = CStr(task.features.Count) + " features were found using '" + task.featureOptions.FeatureMethod.Text + "' method."
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Agast_Close(cPtr)
    End Sub
End Class





' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_NoMotionTest : Inherits TaskParent
    Public options As New Options_Features
    Dim method As New Feature_Basics
    Public Sub New()
        desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent results."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        dst2 = src.Clone

        method.Run(src)

        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next

        labels(2) = method.labels(2)
    End Sub
End Class








' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_KNN : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public featurePoints As New List(Of cv.Point2f)
    Dim feat As New Feature_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find good features to track in a BGR image but use the same point if closer than a threshold"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)

        knn.queries = New List(Of cv.Point2f)(task.features)
        If task.firstPass Then knn.trainInput = New List(Of cv.Point2f)(knn.queries)
        knn.Run(src)

        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = task.features(i)
            If pt.DistanceTo(qPt) > 2 Then knn.trainInput(trainIndex) = task.features(i)
        Next
        featurePoints = New List(Of cv.Point2f)(knn.trainInput)

        src.CopyTo(dst2)
        dst3.SetTo(0)
        For Each pt In featurePoints
            DrawCircle(dst2, pt, task.DotSize + 2, white)
            DrawCircle(dst3, pt, task.DotSize + 2, white)
        Next

        labels(2) = feat.labels(2)
        labels(3) = feat.labels(2)
    End Sub
End Class






Public Class Feature_Delaunay : Inherits TaskParent
    Dim delaunay As New Delaunay_Contours
    Dim feat As New Feature_Basics
    Public Sub New()
        task.featureOptions.DistanceSlider.Value = 10
        desc = "Divide the image into contours with Delaunay using features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)
        dst2 = feat.dst2
        labels(2) = feat.labels(2)

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
    Public Sub New()
        desc = "Provide a trace of the tracked features"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        pyr.Run(src)
        dst2 = pyr.dst2
        labels(2) = pyr.labels(2)

        If task.heartBeat Then dst3.SetTo(0)

        ptList.Clear()
        Dim stationary As Integer, motion As Integer
        For i = 0 To pyr.features.Count - 1
            Dim pt = New cv.Point(pyr.features(i).X, pyr.features(i).Y)
            ptList.Add(pt)
            If ptLast.Contains(pt) Then
                DrawCircle(dst3, pt, task.DotSize, task.highlight)
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
    Dim feat As New Feature_Basics
    Public Sub New()
        labels(3) = "Features found in the image"
        desc = "Use the sorted list of Delaunay regions to find the top X points to track."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)

        If task.heartBeat Then dst2.SetTo(0)

        For Each pt In task.features
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next
        labels(2) = CStr(task.features.Count) + " targets were present with " + CStr(task.FeatureSampleSize) + " requested."
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
        dst3 = features.dst3

        If task.optionsChanged Then goodList.Clear()

        Dim ptList As New List(Of cv.Point2f)(task.features)
        goodList.Add(ptList)

        If goodList.Count >= task.frameHistoryCount Then goodList.RemoveAt(0)

        dst2.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                DrawCircle(task.color, pt, task.DotSize, task.highlight)
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
        OptionParent.FindSlider("Corner normalize threshold").Value = 15
        labels = {"", "", "Features in the left camera image", "Features in the right camera image"}
        desc = "Identify feature points in the left And right views"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If options.useShiTomasi Then
            dst2 = task.leftView
            dst3 = task.rightView
            shiTomasi.Run(task.leftView)
            dst2.SetTo(cv.Scalar.White, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

            shiTomasi.Run(task.rightView)
            dst3.SetTo(task.highlight, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
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
    Dim feat As New Feature_Basics
    Public Sub New()
        desc = "Find feature age maximum and average."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)

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
    Dim feat As New Feature_Basics
    Public Sub New()
        desc = "Find good features across multiple frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)

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
                    DrawCircle(dst2, pt, task.DotSize, task.highlight)
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
    Dim feat As New Feature_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(3) = "Click 'Show grid mask overlay' to see grid boundaries."
        desc = "Find the feature population for each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)
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









Public Class Feature_AKaze : Inherits TaskParent
    Dim kazeKeyPoints As cv.KeyPoint() = Nothing
    Dim kaze As AKAZE
    Public Sub New()
        labels(2) = "AKAZE key points"
        desc = "Find keypoints using AKAZE algorithm."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = src.Clone()
        If src.Channels() <> 1 Then
            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        End If
        If kaze Is Nothing Then kaze = cv.AKAZE.Create()
        Dim kazeDescriptors As New cv.Mat()
        kaze.DetectAndCompute(src, Nothing, kazeKeyPoints, kazeDescriptors)
        For i As Integer = 0 To kazeKeyPoints.Length - 1
            DrawCircle(dst2, kazeKeyPoints(i).Pt, task.DotSize, task.highlight)
        Next
    End Sub
    Public Sub Close()
        If kaze IsNot Nothing Then kaze.Dispose()
    End Sub
End Class





Public Class Feature_RedCloud : Inherits TaskParent
    Dim feat As New Feature_Basics
    Public Sub New()
        desc = "Show the feature points in the RedCloud output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)

        dst2 = runRedC(src, labels(2))

        For Each pt In task.featurePoints
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next
    End Sub
End Class






Public Class Feature_WithDepth : Inherits TaskParent
    Dim feat As New Feature_Basics
    Public Sub New()
        task.brickRunFlag = True
        desc = "Show the feature points that have depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)

        dst2 = src
        Dim depthCount As Integer
        For Each pt In task.featurePoints
            Dim index = task.grid.gridMap.Get(Of Single)(pt.Y, pt.X)
            If task.bricks.brickList(index).depth > 0 Then
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
                depthCount += 1
            End If
        Next
        labels(2) = CStr(depthCount) + " features had depth or " + Format(depthCount / task.features.Count, "0%")
    End Sub
End Class





Public Class Feature_Matching : Inherits TaskParent
    Public features As New List(Of cv.Point)
    Public motionPoints As New List(Of cv.Point)
    Dim match As New Match_Basics
    Dim feat As New Feature_Basics
    Public Sub New()
        task.featureOptions.FeatureSampleSize.Value = 150
        desc = "Use correlation coefficient to keep features from frame to frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static fpLastSrc = src.Clone

        Dim matched As New List(Of cv.Point)
        motionPoints.Clear()
        For Each pt In features
            Dim val = task.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val = 0 Then
                Dim index As Integer = task.grid.gridMap.Get(Of Single)(pt.Y, pt.X)
                Dim r = task.gridRects(index)
                match.template = fpLastSrc(r)
                match.Run(src(r))
                If match.correlation > task.fCorrThreshold Then matched.Add(pt)
            Else
                motionPoints.Add(pt)
            End If
        Next

        labels(2) = "There were " + CStr(features.Count) + " features identified and " + CStr(matched.Count) +
                    " were matched to the previous frame"

        If matched.Count < task.FeatureSampleSize / 2 Then
            feat.Run(src)
            features = task.featurePoints
        Else
            features = New List(Of cv.Point)(matched)
        End If

        dst2 = src.Clone
        For Each pt In features
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next

        fpLastSrc = src.Clone
    End Sub
End Class






Public Class Feature_SteadyCam : Inherits TaskParent
    Public options As New Options_Features
    Dim feat As New Feature_Basics
    Public Sub New()
        OptionParent.FindSlider("Threshold Percent for Resync").Value = 50
        desc = "Track features using correlation without the motion mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        feat.Run(task.grayStable)

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
            Dim index As Integer = task.grid.gridMap.Get(Of Single)(pt.Y, pt.X)
            Dim r = task.gridRects(index)
            cv.Cv2.MatchTemplate(src(r), lastSrc(r), correlationMat, mode)
            If correlationMat.Get(Of Single)(0, 0) >= task.fCorrThreshold Then
                features.Add(pt)
            End If
        Next

        dst2 = src
        For Each pt In features
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next

        lastSrc = src.Clone
        labels(2) = CStr(features.Count) + " features were validated by the correlation coefficient"
    End Sub
End Class







Public Class Feature_FacetPoints : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Dim feat As New Feature_Basics
    Public Sub New()
        desc = "Assign each delaunay point to a RedCell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)
        dst2 = runRedC(src, labels(2))

        delaunay.inputPoints = task.features
        delaunay.Run(src)

        Dim ptList As New List(Of cv.Point)
        For Each facets In delaunay.facetList
            For Each pt In facets
                If pt.X >= 0 And pt.X < dst2.Width And pt.Y >= 0 And pt.Y < dst2.Height Then
                    ptList.Add(New cv.Point(pt.X, pt.Y))
                End If
            Next
        Next

        For Each pt In ptList
            Dim index = task.redC.rcMap.Get(Of Byte)(pt.Y, pt.X)
            If index = 0 Then Continue For
            Dim rc = task.redC.rcList(index)
            Dim val = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
            If val <> 0 Then
                rc.ptFacets.Add(pt)
                task.redC.rcList(index) = rc
            End If
        Next

        For Each rc In task.redC.rcList
            For Each pt In rc.ptFacets
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
            Next
        Next

        If standalone And task.redC.rcList.Count > 0 Then
            task.color.Rectangle(task.rcD.rect, task.highlight, task.lineWidth)
            For Each pt In task.rcD.ptFacets
                DrawCircle(task.color, pt, task.DotSize, task.highlight)
            Next
        End If
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
        options.Run()

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
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next
        labels(2) = $"Found {keypoints.Length} features with agast"
    End Sub
    Public Sub Close()
        If agastFD IsNot Nothing Then agastFD.Dispose()
    End Sub
End Class






Public Class Feature_NoMotion : Inherits TaskParent
    Dim feat As New Feature_Basics
    Public Sub New()
        task.gOptions.UseMotionMask.Checked = False
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find good features to track in a BGR image using the motion mask+"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(task.grayStable)
        dst2 = src.Clone

        dst3.SetTo(0)
        For Each pt In task.featurePoints
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next

        labels(2) = feat.labels(2)
    End Sub
End Class





Public Class Feature_StableVisual : Inherits TaskParent
    Dim noMotion As New Feature_Basics
    Public fpStable As New List(Of fpData)
    Public ptStable As New List(Of cv.Point)
    Public Sub New()
        desc = "Show only features present on this and the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastFeatures As New List(Of cv.Point)(task.featurePoints)

        noMotion.Run(src)
        dst3 = noMotion.dst2

        dst2.SetTo(0)
        Dim stable As New List(Of cv.Point)
        For Each pt In task.featurePoints
            If lastFeatures.Contains(pt) Then
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
                stable.Add(pt)
            End If
        Next
        lastFeatures = New List(Of cv.Point)(stable)
        labels(2) = noMotion.labels(2) + " and " + CStr(stable.Count) + " appeared on earlier frames "

        dst2 = src.Clone
        For Each pt In stable
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next
        labels(3) = "The " + CStr(stable.Count) + " points are present for more than one frame."
    End Sub
End Class





Public Class Feature_StableVisualize : Inherits TaskParent
    Dim noMotion As New Feature_NoMotion
    Public fpStable As New List(Of fpData)
    Public ptStable As New List(Of cv.Point)
    Public Sub New()
        desc = "Identify features that consistently present in the image - with motion ignored."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastFeatures As New List(Of cv.Point)(task.featurePoints)

        noMotion.Run(src)

        dst2 = src
        Dim stable As New List(Of cv.Point)
        For Each pt In task.featurePoints
            If lastFeatures.Contains(pt) Then
                DrawCircle(dst2, pt, task.DotSize, task.highlight)
                stable.Add(pt)
            End If
        Next
        lastFeatures = New List(Of cv.Point)(stable)
        labels(2) = noMotion.labels(2) + " and " + CStr(stable.Count) + " appeared on earlier frames "

        Dim fpNew As New List(Of fpData)
        Dim ptNew As New List(Of cv.Point)
        For Each pt In stable
            Dim fp As New fpData
            If ptStable.Contains(pt) Then
                Dim index = ptStable.IndexOf(pt)
                fp = fpStable(index)
                fp.age += 1
            Else
                fp.age = 1
                fp.pt = pt
            End If

            fp.index = fpNew.Count
            fpNew.Add(fp)
            ptNew.Add(pt)
        Next

        fpStable = New List(Of fpData)(fpNew)
        ptStable = New List(Of cv.Point)(ptNew)

        dst3.SetTo(0)
        For Each fp In fpStable
            If fp.age > 2 Then
                DrawCircle(dst3, fp.pt, task.DotSize, task.highlight)
                SetTrueText(CStr(fp.age), fp.pt, 3)
            End If
        Next
    End Sub
End Class