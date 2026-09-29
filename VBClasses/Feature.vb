Imports System.Runtime.InteropServices : Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Feature_Basics : Inherits TaskParent
        Implements IDisposable
        Public options As New Options_Features
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk..."
        End Sub
        Public Shared Function ToCvPoints(features As List(Of cv.Point)) As List(Of cv.Point)
            Dim pts As New List(Of cv.Point)
            For Each pt In features
                pts.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
            Next
            Return pts
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            dst2 = src.Clone

            If src.Channels <> 1 Then src = task.gray

            Dim ptLatest As New List(Of cv.Point2f)
            Select Case task.fOptions.FeatureMethod.Text
                Case "AGAST"
                    If cPtr = 0 Then cPtr = Agast_Open()
                    src = task.color.Clone
                    Dim dataSrc(src.Total - 1) As Vec3b
                    src.GetArray(Of Vec3b)(dataSrc)

                    Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
                    Dim imagePtr = Agast_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.agastThreshold)
                    handleSrc.Free()

                    Dim ptMat = Mat.FromPixelData(Agast_Count(cPtr), 1, MatType.CV_32FC2, imagePtr).Clone
                    For i = 0 To ptMat.Rows - 1
                        Dim pt = ptMat.Get(Of cv.Point2f)(i, 0)
                        ptLatest.Add(pt)
                        If standaloneTest() Then Circle(dst2, pt, task.DotSize, white, -1, task.lineType)
                    Next

                    strOut = "AGAST produced " + CStr(ptLatest.Count) + " features"
                Case "AKAZE"
                    Static kaze As XFeatures2D.AKAZE = XFeatures2D.AKAZE.Create()
                    Dim kazeKeyPoints As KeyPoint() = Nothing
                    Dim kazeDescriptors As New Mat()
                    kaze.DetectAndCompute(src, Nothing, kazeKeyPoints, kazeDescriptors)
                    For i = 0 To kazeKeyPoints.Length - 1
                        ptLatest.Add(kazeKeyPoints(i).Pt)
                    Next
                Case "BrickPoint_Basics"
                    Static bPoint As New BrickPoint_Basics
                    bPoint.Run(src)
                    For Each pt In bPoint.ptList
                        ptLatest.Add(pt)
                    Next
                    strOut = bPoint.labels(2)
                Case "Feature_BRISK"
                    Static brisk As New Feature_BRISK
                    brisk.Run(src)
                    ptLatest = brisk.features
                    strOut = "BRISK produced " + CStr(ptLatest.Count) + " features"
                Case "Corner_Basics"
                    Static FAST As New Corner_Basics
                    FAST.Run(src)
                    ptLatest = FAST.features
                    strOut = "FAST produced " + CStr(ptLatest.Count) + " features"
                Case "GoodFeatures"
                    ptLatest = GoodFeaturesToTrack(src, task.fOptions.FeatureSizeSlider.Value, options.quality,
                                                      options.minDistance, New Mat,
                                                      options.blockSize, True, options.k).ToList
                    strOut = "GoodFeatures produced " + CStr(ptLatest.Count) + " features"
                Case "Corner_HarrisDetector_CPP"
                    Static harris As New Corner_HarrisDetector_CPP
                    harris.Run(src)
                    ptLatest = harris.features
                    strOut = "Harris Detector produced " + CStr(ptLatest.Count) + " features"
                Case "LineInput"
                    For Each lp In task.lines.lpList
                        ptLatest.Add(lp.ptCenter)
                    Next
            End Select

            lastFeatures = New List(Of cv.Point)(features)
            Dim ptNext As New List(Of cv.Point)
            For Each pt In features
                Dim val = task.motion.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 0 Then ptNext.Add(pt)
            Next

            For Each pt In ptLatest
                Dim val = task.motion.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val <> 0 Then ptNext.Add(pt)
            Next

            If ptNext.Count <= 3 Then
                For Each pt In ptLatest
                    ptNext.Add(pt)
                Next
            End If

            features = New List(Of cv.Point)(ptNext)

            dst3.SetTo(0)
            For Each pt In features
                If lastFeatures.Contains(pt) Then
                    Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                    Circle(dst3, pt, task.DotSize, task.highlight, -1, task.lineType)
                End If
            Next

            If features.Count = 0 Then features = New List(Of cv.Point)(ptNext)

            labels(2) = strOut
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = Agast_Close(cPtr)
        End Sub
    End Class





    Public Class XR_Feature_AKaze : Inherits TaskParent
        Implements IDisposable
        Dim kazeKeyPoints As KeyPoint() = Nothing
        Dim kaze As XFeatures2D.AKAZE
        Public Sub New()
            labels(2) = "AKAZE key points"
            desc = "Find keypoints using AKAZE algorithm."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            dst2 = src.Clone()
            If src.Channels() <> 1 Then src = task.gray
            If kaze Is Nothing Then kaze = XFeatures2D.AKAZE.Create()
            Dim kazeDescriptors As New Mat()
            kaze.DetectAndCompute(src, Nothing, kazeKeyPoints, kazeDescriptors)
            For i = 0 To kazeKeyPoints.Length - 1
                Circle(dst2, kazeKeyPoints(i).Pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
        End Sub
        Protected Overrides Sub Finalize()
            If kaze IsNot Nothing Then kaze.Dispose()
        End Sub
    End Class





    Public Class Feature_GoodFeatures : Inherits TaskParent
        Public options As New Options_Features
        Public features As New List(Of cv.Point)
        Public Sub New()
            task.fOptions.FeatureSizeSlider.Value = 50
            labels(2) = "Good features found with GoodFeaturesToTrack"
            desc = "Cursor.ai: Find good features in the image using OpenCV GoodFeaturesToTrack."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            Dim gray = If(src.Channels() = 1, src, task.gray)
            Dim points = GoodFeaturesToTrack(gray, task.fOptions.FeatureSizeSlider.Value, options.quality,
                                         options.minDistance, New Mat,
                                         options.blockSize, False, options.k)

            features.Clear()
            dst2 = If(src.Channels() = 1, task.color.Clone, src.Clone)
            For Each pt In points
                Dim feature = New cv.Point(CInt(pt.X), CInt(pt.Y))
                features.Add(feature)
                Circle(dst2, feature, task.DotSize, task.highlight, -1, task.lineType)
            Next

            labels(2) = CStr(features.Count) + " good features found; " +
                    CStr(task.fOptions.FeatureSizeSlider.Value) + " requested."
        End Sub
    End Class





    Public Class XR_Feature_Basics : Inherits TaskParent
        Implements IDisposable
        Public options As New Options_Features
        Public features As New List(Of Point2f)
        Public Sub New()
            desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk..."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels <> 1 Then src = task.gray

            dst2 = task.color.Clone

            Dim ptNew As New List(Of Point2f)


            strOut = ""
            Dim ptLatest As New List(Of Point2f)
            Select Case task.fOptions.FeatureMethod.Text
                Case "GoodFeatures"
                    ptLatest = GoodFeaturesToTrack(src, task.fOptions.FrameHistoryCount.Value, options.quality,
                                                      options.minDistance, New Mat,
                                                      options.blockSize, True, options.k).ToList
                    strOut = "GoodFeatures produced " + CStr(ptLatest.Count) + " features"
                Case "AGAST"
                    If cPtr = 0 Then cPtr = Agast_Open()
                    src = task.color.Clone
                    Dim dataSrc(src.Total - 1) As Vec3b
                    src.GetArray(Of Vec3b)(dataSrc)

                    Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
                    Dim imagePtr = Agast_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.agastThreshold)
                    handleSrc.Free()

                    Dim ptMat = Mat.FromPixelData(Agast_Count(cPtr), 1, MatType.CV_32FC2, imagePtr).Clone
                    For i = 0 To ptMat.Rows - 1
                        Dim pt = ptMat.Get(Of Point2f)(i, 0)
                        ptLatest.Add(pt)
                        If standaloneTest() Then Circle(dst2, pt, task.DotSize, white, -1, task.lineType)
                    Next

                    strOut = "GoodFeatures produced " + CStr(ptLatest.Count) + " features"
                Case "BRISK"
                    Static brisk As New Feature_BRISK
                    brisk.Run(src)
                    ptLatest = brisk.features
                    strOut = "GoodFeatures produced " + CStr(ptLatest.Count) + " features"
                Case "Harris"
                    Static harris As New Corner_HarrisDetector_CPP
                    harris.Run(src)
                    ptLatest = harris.features
                    strOut = "Harris Detector produced " + CStr(ptLatest.Count) + " features"
                Case "FAST"
                    Static FAST As New Corner_Basics
                    FAST.Run(src)
                    ptLatest = FAST.features
                    strOut = "FAST produced " + CStr(ptLatest.Count) + " features"
                Case "LineInput"
                    For Each lp In task.lines.lpList
                        ptLatest.Add(lp.ptCenter)
                    Next
                Case "BrickPoint"
                    Static bPoint As New BrickPoint_Basics
                    bPoint.Run(src)
                    For Each pt In bPoint.ptList
                        ptLatest.Add(pt)
                    Next
                    strOut = bPoint.labels(2)
            End Select

            If task.optionsChanged Or ptNew.Count = 0 Then
                For Each pt In ptLatest
                    ptNew.Add(pt)
                Next
            Else
                For Each pt In ptLatest
                    Dim val = task.motion.motionMask.Get(Of Byte)(pt.Y, pt.X)
                    If val = 255 Then ptNew.Add(pt)
                Next
            End If

            Dim sortByGrid As New SortedList(Of Single, Point2f)(New compareAllowIdenticalSingle)
            For Each pt In ptNew
                Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                sortByGrid.Add(index, pt)
            Next

            features = New List(Of Point2f)(sortByGrid.Values)

            For Each pt In features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            labels(2) = strOut
        End Sub
        Protected Overrides Sub Finalize()
            If cPtr <> 0 Then cPtr = Agast_Close(cPtr)
        End Sub
    End Class






    Public Class Feature_Bricks : Inherits TaskParent
        Public features As New List(Of cv.Point)
        Public feature2f As New List(Of Point2f)
        Dim bPoint As New XR_BrickPoint_MaxSobel
        Public Sub New()
            desc = "Gather features from the sobel grid square points and preserve those representing lines."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastFeatures As New List(Of cv.Point)(bPoint.features)
            bPoint.Run(src)

            dst2 = src.Clone
            feature2f.Clear()
            For Each pt In bPoint.features
                Dim index = lastFeatures.IndexOf(pt)
                If index >= 0 Then feature2f.Add(New Point2f(pt.X, pt.Y))
            Next

            features.Clear()
            For Each pt In feature2f
                features.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            strOut = CStr(features.Count) + " features found ('BrickPoints' method). " +
                         CStr(bPoint.features.Count - feature2f.Count) + " dropped."
            labels(2) = strOut
        End Sub
    End Class






    Public Class Feature_Delaunay : Inherits TaskParent
        Dim delaunay As New Delaunay_Contours
        Public feat As New Feature_Basics
        Dim options As New Options_Features
        Public Sub New()
            OptionParent.FindSlider("Min Distance").Value = 10
            desc = "Divide the image into contours with Delaunay using features"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            feat.Run(src)
            labels(2) = feat.labels(2)

            dst2 = src
            For Each pt In feat.features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            delaunay.bPoint.ptList = New List(Of cv.Point)(feat.features)
            delaunay.Run(src)
            dst3 = delaunay.dst2
            For Each pt In delaunay.bPoint.ptList
                Circle(dst3, pt, task.DotSize, white, -1, task.lineType)
            Next
            labels(3) = "There were " + CStr(feat.features.Count) + " Delaunay contours"
        End Sub
    End Class







    ' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
    Public Class XR_Feature_NoMotionTest : Inherits TaskParent
        Public options As New Options_Features
        Dim feat As New Feature_Basics
        Public Sub New()
            desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent sharedResults.images.."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()
            dst2 = src.Clone

            feat.Run(src)

            For Each pt In feat.features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            labels(2) = feat.labels(2)
        End Sub
    End Class






    Public Class XR_Feature_LucasKanade : Inherits TaskParent
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
                    Circle(dst3, pt, task.DotSize, task.highlight, -1, task.lineType)
                    stationary += 1
                Else
                    Line(dst3, pyr.lastFeatures(i), pyr.features(i), white, task.lineWidth, task.lineType)
                    motion += 1
                End If
            Next

            labels(3) = CStr(stationary) + " features were stationary and " + CStr(motion) + " features had some motion."
            ptLast = New List(Of cv.Point)(ptList)
        End Sub
    End Class







    Public Class XR_Feature_TraceDelaunay : Inherits TaskParent
        Dim features As New Feature_Delaunay
        Public goodList As New List(Of List(Of Point2f)) ' stable points only
        Public Sub New()
            labels = {"Stable points highlighted", "", "", "Delaunay map of regions defined by the feature points"}
            desc = "Trace the GoodFeatures points using only Delaunay - no KNN or RedCloud or Matching."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            features.Run(src)
            dst3 = features.dst3

            If task.optionsChanged Then goodList.Clear()

            Dim ptList As New List(Of Point2f)
            For Each pt In features.feat.features
                ptList.Add(pt)
            Next
            goodList.Add(ptList)

            If goodList.Count >= task.fOptions.FrameHistoryCount.Value Then goodList.RemoveAt(0)

            dst2.SetTo(0)
            For Each ptList In goodList
                For Each pt In ptList
                    Circle(task.color, pt, task.DotSize, task.highlight, -1, task.lineType)
                    Dim c = dst3.Get(Of Vec3b)(pt.Y, pt.X)
                    Circle(dst2, pt, task.DotSize + 1, c, -1, task.lineType)
                Next
            Next
            labels(2) = CStr(ptList.Count) + " features were identified in the image."
        End Sub
    End Class






    Public Class XR_Feature_ShiTomasi : Inherits TaskParent
        Dim harris As New Corner_HarrisDetector_CPP
        Dim shiTomasi As New Corner_ShiTomasi_CPP
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
                Dim _cvtInline As New Mat
                CvtColor(shiTomasi.dst3, _cvtInline, ColorConversionCodes.BGR2GRAY)
                dst2.SetTo(Scalar.White, _cvtInline)

                shiTomasi.Run(task.rightView)
                CvtColor(shiTomasi.dst3, _cvtInline, ColorConversionCodes.BGR2GRAY)
                dst3.SetTo(task.highlight, _cvtInline)
            Else
                harris.Run(task.leftView)
                dst2 = harris.dst2.Clone
                harris.Run(task.rightView)
                dst3 = harris.dst2
            End If
        End Sub
    End Class






    Public Class XR_Feature_Generations : Inherits TaskParent
        Dim features As New List(Of cv.Point)
        Dim gens As New List(Of Integer)
        Dim feat As New Feature_Basics
        Public Sub New()
            desc = "Find feature age maximum and average."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.gray)

            Dim newfeatures As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
            For Each pt In feat.features
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
                Circle(dst2, pt, task.DotSize, white, -1, task.lineType)
            Next

            If task.heartBeat And gens.Count > 0 Then
                labels(2) = CStr(features.Count) + " features found with max/average " + CStr(gens(0)) + "/" + gens.Average.ToString(fmt0) + " generations"
            End If
        End Sub
    End Class




    ' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
    Public Class XR_Feature_History : Inherits TaskParent
        Public features As New List(Of cv.Point)
        Dim featureHistory As New List(Of List(Of cv.Point))
        Dim gens As New List(Of Integer)
        Dim feat As New Feature_Basics
        Public Sub New()
            desc = "Find good features across multiple frames."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.gray)

            dst2 = src.Clone

            featureHistory.Add(New List(Of cv.Point)(feat.features))

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

            Dim threshold = If(task.fOptions.FrameHistoryCount.Value = 1, 0, 1)
            features.Clear()
            Dim whiteCount As Integer
            For i = 0 To newFeatures.Count - 1
                If gens(i) > threshold Then
                    Dim pt = newFeatures(i)
                    features.Add(pt)
                    If gens(i) < task.fOptions.FrameHistoryCount.Value Then
                        Circle(dst2, pt, task.DotSize + 2, Scalar.Red, -1, task.lineType)
                    Else
                        whiteCount += 1
                        Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                    End If
                End If
            Next

            If featureHistory.Count > task.fOptions.FrameHistoryCount.Value Then featureHistory.RemoveAt(0)
            If task.heartBeat Then
                labels(2) = CStr(features.Count) + "/" + CStr(whiteCount) + " present/present on every frame" +
                            " red is a recent addition, yellow is present on previous " +
                            CStr(task.fOptions.FrameHistoryCount.Value) + " frames"
            End If
        End Sub
    End Class






    Public Class XR_Feature_RedC : Inherits TaskParent
        Dim feat As New Feature_Basics
        Dim redC As New RedColor_BasicsOld
        Public Sub New()
            desc = "Show the feature points in the RedC output."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(src)

            redC.Run(src)
            dst2 = redC.dst2
            labels(2) = redC.labels(2)

            For Each pt In feat.features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
        End Sub
    End Class






    Public Class XR_Feature_WithDepth : Inherits TaskParent
        Dim bricks As New Brick_Basics
        Dim feat As New Feature_Basics
        Public Sub New()
            desc = "Show the feature points that have depth."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            bricks.Run(src)
            feat.Run(task.gray)

            dst2 = src
            Dim depthCount As Integer
            For Each pt In feat.features
                Dim index = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                If bricks.brickList(index).depth > 0 Then
                    Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                    depthCount += 1
                End If
            Next
            labels(2) = CStr(depthCount) + " features had depth or " + (depthCount / feat.features.Count).ToString("0%")
        End Sub
    End Class







    Public Class XR_Feature_SteadyCam : Inherits TaskParent
        Public options As New Options_Features
        Dim feat As New Feature_Basics
        Public Sub New()
            OptionParent.FindSlider("Threshold Percent for Resync").Value = 50
            desc = "Track features using correlation without the motion mask"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            feat.Run(task.gray)

            Static features As New List(Of cv.Point)(feat.features)
            Static lastSrc As Mat = src.Clone

            Dim resync = features.Count / feat.features.Count < options.resyncThreshold
            If task.heartBeat Or task.optionsChanged Or resync Then
                features = New List(Of cv.Point)(feat.features)
            End If

            Dim ptList = New List(Of cv.Point)(features)
            Dim correlationMat As New Mat
            Dim mode = TemplateMatchModes.CCoeffNormed
            features.Clear()
            For Each pt In ptList
                Dim index As Integer = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim r = task.gridRects(index)
                MatchTemplate(src(r), lastSrc(r), correlationMat, mode)
                If correlationMat.Get(Of Single)(0, 0) >= task.fCorrThreshold Then
                    features.Add(pt)
                End If
            Next

            dst2 = src
            For Each pt In features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            lastSrc = src.Clone
            labels(2) = CStr(features.Count) + " features were validated by the correlation coefficient"
        End Sub
    End Class






    Public Class XR_Feature_Agast : Inherits TaskParent
        Implements IDisposable
        Dim agastFD As XFeatures2D.AgastFeatureDetector
        Dim stablePoints As New List(Of Point2f)
        Dim options As New Options_Agast
        Public Sub New()
            desc = "Use the Agast Feature Detector in the OpenCV Contrib."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If task.optionsChanged Then
                If agastFD IsNot Nothing Then agastFD.Dispose()
                agastFD = XFeatures2D.AgastFeatureDetector.Create(options.agastThreshold, options.useNonMaxSuppression,
                                                         XFeatures2D.AgastFeatureDetector.DetectorType.OAST_9_16)
            End If

            Dim keypoints As KeyPoint() = agastFD.Detect(src)

            Dim currPoints As New List(Of Point2f)
            For Each kpt As KeyPoint In keypoints
                currPoints.Add(kpt.Pt)
            Next

            Dim newList As New List(Of Point2f)
            For Each pt In stablePoints
                Dim val = task.motion.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 0 Then newList.Add(pt)
            Next

            For Each pt In currPoints
                Dim val = task.motion.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val <> 0 Then newList.Add(pt)
            Next

            stablePoints = New List(Of Point2f)(newList)
            dst2 = src
            For Each pt In stablePoints
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(2) = $"Found {keypoints.Length} features with agast"
        End Sub
        Protected Overrides Sub Finalize()
            If agastFD IsNot Nothing Then agastFD.Dispose()
        End Sub
    End Class






    Public Class XR_Feature_BrickLine : Inherits TaskParent
        Public features As New List(Of cv.Point)
        Dim feat As New Feature_Bricks
        Public Sub New()
            task.gOptions.LineWidth.Value = 3
            desc = "Find the lines implied in the grid square points."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim sortByGrid As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
            For Each pt In feat.features
                Dim lineIndex = task.lines.dst1.Get(Of Byte)(pt.Y, pt.X)
                If lineIndex = 0 Then Continue For
                Dim gridindex = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
                sortByGrid.Add(gridindex, pt)
            Next

            Dim brickLines(task.lines.lpList.Count - 1) As List(Of cv.Point)
            dst3.SetTo(0)
            features.Clear()
            For Each pt In sortByGrid.Values
                Dim lineIndex = task.lines.dst1.Get(Of Byte)(pt.Y, pt.X) - 1
                If brickLines(lineIndex) Is Nothing Then
                    brickLines(lineIndex) = New List(Of cv.Point)({pt})
                Else
                    brickLines(lineIndex).Add(pt)
                End If

                features.Add(pt)
            Next

            dst2 = src.Clone
            For i = 0 To brickLines.Length - 1
                If brickLines(i) Is Nothing Then Continue For
                If brickLines.Length = 1 Then Continue For
                Dim pt = brickLines(i)(0)
                If pt = brickLines(i).Last Then Continue For
                Dim color = vecToScalar(task.lines.dst2.Get(Of Vec3b)(pt.Y, pt.X))
                Circle(dst3, pt, task.DotSize, color, -1, task.lineType)
                Line(dst2, pt, brickLines(i).Last, color, task.lineWidth, task.lineType)
                Line(dst3, pt, brickLines(i).Last, color, task.lineWidth, task.lineType)
            Next
        End Sub
    End Class






    Public Class Feature_StableVisual : Inherits TaskParent
        Dim feat As New Feature_Basics
        Public fpStable As New List(Of fpData)
        Public ptStable As New List(Of cv.Point)
        Public Sub New()
            desc = "Show only features present on this and the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim lastFeatures As New List(Of cv.Point)(feat.features)

            feat.Run(src)
            dst3 = feat.dst2

            dst2.SetTo(0)
            lastFeatures.Clear()
            For Each pt In feat.features
                If lastFeatures.Contains(pt) Then
                    Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                    lastFeatures.Add(pt)
                End If
            Next

            labels(2) = feat.labels(2) + " and " + CStr(lastFeatures.Count) + " appeared on earlier frames "

            dst2 = src.Clone
            For Each pt In lastFeatures
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(3) = "The " + CStr(lastFeatures.Count) + " points are present for more than one frame."
        End Sub
    End Class






    ' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
    Public Class Feature_KNN : Inherits TaskParent
        Dim knn As New KNN_Basics
        Public feat As New Feature_Basics
        Public Sub New()
            dst3 = New Mat(dst3.Size(), MatType.CV_8U, Scalar.All(0))
            desc = "Find good features to track in the image but use the same cv.Point if closer than a threshold"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.gray)

            knn.ptListQuery = New List(Of cv.Point)(feat.features)
            If knn.ptListTrain.Count = 0 Or task.gOptions.DebugCheckBox.Checked Then
                knn.ptListTrain = New List(Of cv.Point)(knn.ptListQuery)
                task.gOptions.DebugCheckBox.Checked = False
            End If

            knn.Run(src)

            For i = 0 To knn.queries.Count - 1
                Dim trainIndex = knn.result(i, 0) ' index of the matched train input
                Dim pt = knn.ptListTrain(trainIndex)
                Dim qPt = feat.features(i)
                If pt.DistanceTo(qPt) > 2 Then knn.ptListTrain(trainIndex) = feat.features(i)
            Next

            src.CopyTo(dst2)
            dst3.SetTo(0)
            For Each pt In feat.features
                Circle(dst2, pt, task.DotSize + 2, white, -1, task.lineType)
                Circle(dst3, pt, task.DotSize + 2, white, -1, task.lineType)
            Next

            labels(2) = feat.labels(2)
            labels(3) = feat.labels(2)
        End Sub
    End Class





    Public Class XR_Feature_LeftRight : Inherits TaskParent
        Dim pyrLeft As New Feature_Basics
        Dim pyrRight As New Feature_Basics
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            desc = "Find features in both the left and right images."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            pyrLeft.Run(task.leftView)
            pyrRight.Run(task.rightView)

            Dim ptLeft As New List(Of cv.Point)
            CvtColor(task.leftView, dst2, ColorConversionCodes.GRAY2BGR)
            Dim rightMatches As New List(Of cv.Point)
            For i = 0 To pyrLeft.features.Count - 1
                Dim pt = pyrLeft.features(i)
                Dim depth = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
                If depth > 0 Then
                    ptLeft.Add(New cv.Point(pt.X, pt.Y))
                    Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                    Dim ptRight = pt
                    ptRight.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth
                    rightMatches.Add(ptRight)
                End If
            Next

            CvtColor(task.rightView, dst3, ColorConversionCodes.GRAY2BGR)
            lastFeatures = New List(Of cv.Point)(features)
            Dim newFeatures As New List(Of cv.Point)
            For Each pt In pyrRight.features
                Dim index = rightMatches.IndexOf(pt)
                If index >= 0 Then
                    newFeatures.Add(ptLeft(index))
                    Circle(dst2, ptLeft(index), task.DotSize + 2, task.highlight, -1, task.lineType)
                    Circle(dst3, pt, task.DotSize + 2, task.highlight, -1, task.lineType)
                End If
            Next

            If lastFeatures.Count > 0 Then
                features.Clear()
                For Each pt In newFeatures
                    If lastFeatures.Contains(pt) = False Then features.Add(pt)
                Next
            Else
                features = New List(Of cv.Point)(newFeatures)
                lastFeatures = New List(Of cv.Point)(features)
            End If

            If task.quarterBeat Then
                labels(2) = CStr(ptLeft.Count) + " features found in the left image."
                labels(3) = CStr(features.Count) + " were confirmed present for 2 frames in both the left and right images."
            End If
        End Sub
    End Class






    Public Class XR_Feature_LeftRightCorrelation : Inherits TaskParent
        Dim feat As New Feature_Basics
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Dim match As New Match_Basics
        Public Sub New()
            task.fOptions.MatchCorrSlider.Value = 75
            desc = "Find features in the left image and verify them with correlation."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim maxCorr = task.fOptions.MatchCorrSlider.Value / 100
            feat.Run(task.leftView)

            lastFeatures = New List(Of cv.Point)(features)
            features.Clear()
            dst2 = task.color.Clone
            dst3 = task.color.Clone
            Dim countNoDepth As Integer, countNoCorr As Integer
            For Each pt In feat.features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
                Dim depth = task.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
                If depth > 0 Then
                    Dim rect = task.gridRects(task.gridMap.Get(Of Integer)(pt.Y, pt.X))
                    match.template = task.leftView(rect)
                    rect.X -= task.calibData.baseline * task.calibData.leftIntrinsics.fx / depth
                    rect = ValidateRect(rect)
                    match.Run(task.rightView(rect))
                    If match.correlation > maxCorr Then
                        features.Add(New cv.Point(pt.X, pt.Y))
                        Circle(dst2, pt, task.DotSize + 2, task.highlight, -1, task.lineType)
                        Circle(dst3, pt, task.DotSize + 1, task.highlight, -1, task.lineType)
                    Else
                        countNoCorr += 1
                    End If
                Else
                    countNoDepth += 1
                End If
            Next

            If task.quarterBeat Then
                labels(2) = CStr(features.Count) + " of " + CStr(feat.features.Count) +
                        " features had depth and high correlation to the right image."
                labels(3) = CStr(countNoDepth) + " features had no depth and " +
                        CStr(countNoCorr) + " missed the correlation threshold of " + maxCorr.ToString(fmt2)
            End If
        End Sub
    End Class






    Public Class XR_Feature_Matching : Inherits TaskParent
        Public features As New List(Of cv.Point)
        Public motionPoints As New List(Of cv.Point)
        Dim match As New Match_Basics
        Dim feat As New Feature_Basics
        Public Sub New()
            task.fOptions.FeatureSizeSlider.Value = 150
            desc = "Use correlation coefficient to keep features from frame to frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Static fpLastSrc = src.Clone

            Dim matched As New List(Of cv.Point)
            motionPoints.Clear()
            For Each pt In features
                Dim val = task.motion.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 0 Then
                    Dim index As Integer = task.gridMap.Get(Of Integer)(pt.Y, pt.X)
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

            If matched.Count < task.fOptions.FeatureSizeSlider.Value / 2 Then
                feat.Run(src)
                features = feat.features
            Else
                features = New List(Of cv.Point)(matched)
            End If

            dst2 = src.Clone
            For Each pt In features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            fpLastSrc = src.Clone
        End Sub
    End Class




    Public Class Feature_BRISK : Inherits TaskParent
        Implements IDisposable
        Dim brisk As XFeatures2D.BRISK
        Public features As New List(Of Point2f)
        Dim options As New Options_Features
        Public Sub New()
            brisk = XFeatures2D.BRISK.Create()
            desc = "Detect features with BRISK"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            src.CopyTo(dst2)

            Dim keyPoints = brisk.Detect(src)

            features.Clear()
            For Each pt In keyPoints
                If pt.Size > options.minDistance Then
                    features.Add(New Point2f(pt.Pt.X, pt.Pt.Y))
                    Circle(dst2, pt.Pt, task.DotSize + 1, task.highlight, -1, task.lineType)
                End If
            Next
            labels(2) = CStr(features.Count) + " features found with BRISK"
        End Sub
        Protected Overrides Sub Finalize()
            brisk.Dispose()
        End Sub
    End Class





    Public Class Feature_CheckAll : Inherits TaskParent
        Dim featAgast As New Feature_Basics
        Dim featAkaze As New Feature_Basics
        Public ptList As New List(Of cv.Point)
        Public Sub New()
            desc = "Cursor.ai: Run Feature_Basics with AGAST and AKAZE and keep points found in both."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim saveMethod = task.fOptions.FeatureMethod.SelectedItem
            Dim saveChanged = task.optionsChanged

            task.fOptions.FeatureMethod.SelectedItem = "AGAST"
            featAgast.Run(src)
            Dim agastPts = Feature_Basics.ToCvPoints(featAgast.features)

            task.fOptions.FeatureMethod.SelectedItem = "AKAZE"
            featAkaze.Run(src)
            Dim akazePts = Feature_Basics.ToCvPoints(featAkaze.features)

            If saveMethod IsNot Nothing Then task.fOptions.FeatureMethod.SelectedItem = saveMethod
            task.optionsChanged = saveChanged

            Dim akazeSet As New HashSet(Of cv.Point)(akazePts)

            ptList.Clear()
            For Each pt In agastPts
                If akazeSet.Contains(pt) Then
                    If ptList.Contains(pt) = False Then ptList.Add(pt)
                End If
            Next

            dst2 = If(src.Channels() = 1, task.color.Clone, src.Clone)
            For Each pt In ptList
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            labels(2) = CStr(ptList.Count) + " points found in both AGAST and AKAZE"
            labels(3) = "AGAST " + CStr(agastPts.Count) + ", AKAZE " + CStr(akazePts.Count)
        End Sub
    End Class





    Public Class Feature_LeftRight : Inherits TaskParent
        Dim featLeft As New Feature_Basics
        Dim featRight As New Feature_Basics
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            task.gOptions.showMyDst0.Checked = True
            task.gOptions.showMyDst1.Checked = True
            labels(0) = "Left view - highlights are the best points in the left view."
            labels(1) = "Right view - highlights are the best points in the right view."
            desc = "Cursor.ai: Find Feature_Basics points in the left image and match each to the nearest feature in the right image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            featLeft.Run(task.leftView)
            featRight.Run(task.rightView)
            If featLeft.features.Count = 0 Or featRight.features.Count = 0 Then Exit Sub

            If task.leftView.Channels() = 1 Then
                CvtColor(task.leftView, dst2, ColorConversionCodes.GRAY2BGR)
            Else
                dst2 = task.leftView.Clone
            End If
            For Each pt In featLeft.features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next

            features.Clear()
            lastFeatures.Clear()
            Dim leftKp(featLeft.features.Count - 1) As KeyPoint
            Dim rightKp(featRight.features.Count - 1) As KeyPoint
            For i = 0 To featLeft.features.Count - 1
                Dim pt = featLeft.features(i)
                leftKp(i) = New KeyPoint(pt.X, pt.Y, 1)
            Next
            For i = 0 To featRight.features.Count - 1
                Dim pt = featRight.features(i)
                rightKp(i) = New KeyPoint(pt.X, pt.Y, 1)
            Next

            Dim maxDx = CSng(task.leftView.Width) * 0.2F
            Dim filtered As New List(Of DMatch)
            For i = 0 To featLeft.features.Count - 1
                Dim pLeft = featLeft.features(i)
                Dim bestJ = -1
                Dim bestDist As Single = Single.MaxValue
                For j = 0 To featRight.features.Count - 1
                    Dim pRight = featRight.features(j)
                    If Math.Abs(pLeft.Y - pRight.Y) > 2 Then Continue For
                    If Math.Abs(pLeft.X - pRight.X) > maxDx Then Continue For
                    If pLeft.X < pRight.X Then Continue For ' left cannot be to the right.
                    Dim dist = CSng(pLeft.DistanceTo(pRight))
                    If dist < bestDist Then
                        bestDist = dist
                        bestJ = j
                    End If
                Next
                If bestJ >= 0 Then filtered.Add(New DMatch(i, bestJ, bestDist))
            Next

            If task.rightView.Channels() = 1 Then
                CvtColor(task.leftView, dst3, ColorConversionCodes.GRAY2BGR)
                CvtColor(task.rightView, dst1, ColorConversionCodes.GRAY2BGR)
            Else
                dst3 = task.leftView.Clone
            End If
            FeatureMatch_Basics.DisplayMatches(dst3, filtered, leftKp, rightKp, features, lastFeatures)

            labels(2) = CStr(featLeft.features.Count) + " Feature_Basics points in the left image"
            labels(3) = CStr(filtered.Count) + " matching points.  Red dots are from the right image.  Yellow points are from left image. "

            For Each pt In features
                Circle(dst1, pt, task.DotSize + 1, task.highlight, -1)
            Next

            dst0 = task.color.Clone
            For Each pt In lastFeatures
                Circle(dst0, pt, task.DotSize, task.highlight, -1)
            Next
        End Sub
    End Class





    Public Class Feature_LeftRightAKaze : Inherits TaskParent
        Implements IDisposable
        Dim akaze As XFeatures2D.AKAZE
        Dim matcher As BFMatcher
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            task.gOptions.showMyDst0.Checked = True
            task.gOptions.showMyDst1.Checked = True
            akaze = XFeatures2D.AKAZE.Create()
            matcher = New BFMatcher(NormTypes.Hamming, crossCheck:=False)
            labels(0) = "Left view - highlights are the best points in the left view."
            labels(1) = "Right view - highlights are the best points in the right view."
            desc = "Cursor.ai: Find AKAZE features in the left image and match each to the nearest feature in the right image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim leftKp As KeyPoint() = Nothing
            Dim rightKp As KeyPoint() = Nothing
            Dim leftDesc As New Mat()
            Dim rightDesc As New Mat()
            akaze.DetectAndCompute(task.leftView, Nothing, leftKp, leftDesc)
            akaze.DetectAndCompute(task.rightView, Nothing, rightKp, rightDesc)

            If task.leftView.Channels() = 1 Then
                CvtColor(task.leftView, dst2, ColorConversionCodes.GRAY2BGR)
            Else
                dst2 = task.leftView.Clone
            End If
            If leftKp IsNot Nothing Then
                For Each kp In leftKp
                    Circle(dst2, kp.Pt, task.DotSize, task.highlight, -1, task.lineType)
                Next
            End If

            features.Clear()
            lastFeatures.Clear()
            If Not leftDesc.Empty() AndAlso Not rightDesc.Empty() AndAlso
               leftKp IsNot Nothing AndAlso leftKp.Length > 0 AndAlso
               rightKp IsNot Nothing AndAlso rightKp.Length > 0 Then

                Dim knn = matcher.KnnMatch(leftDesc, rightDesc, k:=2)
                Dim matches = FeatureMatch_Basics.getMatches(knn)
                Dim filtered As New List(Of DMatch)
                For Each m In matches
                    Dim pLeft = leftKp(m.QueryIdx).Pt
                    Dim pRight = rightKp(m.TrainIdx).Pt
                    If Math.Abs(pLeft.Y - pRight.Y) <= 2 Then filtered.Add(m)
                Next

                If task.rightView.Channels() = 1 Then
                    CvtColor(task.leftView, dst3, ColorConversionCodes.GRAY2BGR)
                    CvtColor(task.rightView, dst1, ColorConversionCodes.GRAY2BGR)
                Else
                    dst3 = task.leftView.Clone
                End If
                FeatureMatch_Basics.DisplayMatches(dst3, filtered, leftKp, rightKp, features, lastFeatures)

                labels(2) = CStr(leftKp.Length) + " AKAZE features in the left image"
                labels(3) = CStr(filtered.Count) + " matching points in the right image are RED.  Yellow points are from left image. "

                For Each pt In features
                    Circle(dst1, pt, task.DotSize, task.highlight, -1)
                Next

                dst0 = task.color.Clone
                For Each pt In lastFeatures
                    Circle(dst0, pt, task.DotSize, task.highlight, -1)
                Next
            End If

            leftDesc.Dispose()
            rightDesc.Dispose()
        End Sub
        Protected Overrides Sub Finalize()
            If akaze IsNot Nothing Then akaze.Dispose()
            If matcher IsNot Nothing Then matcher.Dispose()
        End Sub
    End Class





    Public Class Feature_DelaunayRC : Inherits TaskParent
        Dim featDel As New Feature_Delaunay
        Dim delaunay As New Delaunay_Basics
        Public rcList As New List(Of rcDataOld)
        Public rcIndexMap As New Mat(dst2.Size, MatType.CV_32F, 0)
        Public Sub New()
            delaunay.useFeatures = False
            If standalone Then task.gOptions.showMyDst1.Checked = True
            desc = "Cursor.ai: Build rcDataOld from each Feature_Delaunay cell using the cell rect and a filled mask."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            featDel.Run(src)
            dst3 = featDel.dst3
            labels(3) = featDel.labels(3)

            delaunay.ptList.Clear()
            For Each pt In featDel.feat.features
                delaunay.ptList.Add(New cv.Point2f(pt.X, pt.Y))
            Next
            If delaunay.ptList.Count = 0 Then
                rcList.Clear()
                rcIndexMap.SetTo(0)
                Exit Sub
            End If
            delaunay.Run(src)

            Dim rcListLast = New List(Of rcDataOld)(rcList)
            Dim rcIndexMapLast = rcIndexMap.Clone
            Dim usedList As New List(Of Single)
            Dim reusedIndex As Integer

            rcIndexMap.SetTo(0)
            rcList.Clear()
            Dim cellMask As New Mat(dst2.Size, MatType.CV_8U, 0)
            For i = 0 To delaunay.facetList.Count - 1
                Dim facet = delaunay.facetList(i)
                If facet.Count < 3 Then Continue For

                cellMask.SetTo(0)
                FillConvexPoly(cellMask, facet, 255, LineTypes.Link4)
                If CountNonZero(cellMask) = 0 Then Continue For

                Dim nz As New Mat
                FindNonZero(cellMask, nz)
                Dim rect = ValidateRect(BoundingRect(nz))

                Dim rc As New rcDataOld(cellMask(rect), rect, 255)
                If rc.pixels = 0 Then Continue For

                Dim prevPt = rc.maxDist
                If i < delaunay.ptList.Count Then
                    Dim p = delaunay.ptList(i)
                    prevPt = New cv.Point(CInt(p.X), CInt(p.Y))
                End If
                If prevPt.X < 0 Then prevPt.X = 0
                If prevPt.Y < 0 Then prevPt.Y = 0
                If prevPt.X >= rcIndexMapLast.Width Then prevPt.X = rcIndexMapLast.Width - 1
                If prevPt.Y >= rcIndexMapLast.Height Then prevPt.Y = rcIndexMapLast.Height - 1

                Dim previousIndex = rcIndexMapLast.Get(Of Single)(prevPt.Y, prevPt.X)
                If previousIndex <> 0 AndAlso usedList.Contains(previousIndex) = False Then
                    rc.index = previousIndex
                    usedList.Add(rc.index)
                    reusedIndex += 1
                    Dim rcLast = RedC_BasicsOld.rcIndexFind(rcListLast, CInt(rc.index))
                    If rcLast IsNot Nothing Then
                        rc.age = rcLast.age + 1
                        If rc.age >= 1000 Then rc.age = 100
                    Else
                        rc.age = 1
                    End If
                End If
                rcList.Add(rc)
            Next

            Dim nextIndex As Integer = 1
            For Each rc In rcList
                If rc.index = 0 Then
                    While usedList.Contains(nextIndex) Or nextIndex Mod 255 = 0
                        nextIndex += 1
                    End While
                    rc.index = nextIndex
                    usedList.Add(nextIndex)
                    rc.age = 1
                End If
                rc.mapID = rc.index
                rcIndexMap(rc.rect).SetTo(rc.index Mod 255, rc.mask)
            Next

            dst2 = Palettize(rcIndexMap)
            For Each pt In featDel.feat.features
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(2) = CStr(rcList.Count) + " rcDataOld cells from Feature_Delaunay, " +
                        CStr(reusedIndex) + " kept the same color"

            Dim clickIndex = CInt(rcIndexMap.Get(Of Single)(task.clickPoint.Y, task.clickPoint.X))
            Dim selected As rcDataOld = Nothing
            For Each rc In rcList
                If rc.index = clickIndex Then
                    selected = rc
                    Exit For
                End If
            Next

            If selected IsNot Nothing Then
                task.rcDold = selected
                task.color(task.rcDold.rect).SetTo(white, task.rcDold.mask)
                Rectangle(task.color, task.rcDold.rect, task.highlight, task.lineWidth)
                Rectangle(dst2, task.rcDold.rect, task.highlight, task.lineWidth)
                Circle(dst2, task.rcDold.maxDist, task.DotSize + 1, white, -1)
                strOut = task.rcDold.displayCell
                SetTrueText(strOut, 1)
            End If
        End Sub
    End Class
End Namespace
