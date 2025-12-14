Imports System.Runtime.InteropServices
Imports OpenCvSharp
Imports VBClasses.OptionParent
Imports cv = OpenCvSharp
Public Class Feature_Basics : Inherits TaskParent
    Public features As New List(Of cv.Point)
    Public feature2f As New List(Of cv.Point2f)
    Dim bPoint As New BrickPoint_Minimum
    Public Sub New()
        desc = "Gather features from the sobel brick points and preserve those representing lines."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastFeatures As New List(Of cv.Point)(bPoint.features)
        bPoint.Run(src)

        Dim count As Integer
        dst2 = src.Clone
        feature2f.Clear()
        For Each pt In bPoint.features
            Dim index = lastFeatures.IndexOf(pt)
            If index >= 0 Then
                feature2f.Add(New cv.Point2f(pt.X, pt.Y))
            Else
                count += 1
            End If
        Next

        features.Clear()
        For Each pt In feature2f
            features.Add(New cv.Point(CInt(pt.X), CInt(pt.Y)))
            DrawCircle(dst2, pt)
        Next

        strOut = CStr(features.Count) + " features were found using 'BrickPoints' method. " +
                 CStr(count) + " features were skipped."
        If algTask.heartBeat Then labels(2) = strOut
    End Sub
End Class






Public Class Feature_BrickLine : Inherits TaskParent
    Public features As New List(Of cv.Point)
    Public Sub New()
        algTask.gOptions.LineWidth.Value = 3
        If algTask.feat Is Nothing Then algTask.feat = New Feature_Basics
        desc = "Find the lines implied in the brick points."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim sortByGrid As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalInteger)
        For Each pt In algTask.feat.features
            Dim lineIndex = algTask.lines.dst1.Get(Of Byte)(pt.Y, pt.X)
            If lineIndex = 0 Then Continue For
            Dim gridindex = algTask.gridMap.Get(Of Integer)(pt.Y, pt.X)
            sortByGrid.Add(gridindex, pt)
        Next

        Dim brickLines(algTask.lines.lpList.Count - 1) As List(Of cv.Point)
        dst3.SetTo(0)
        features.Clear()
        For Each pt In sortByGrid.Values
            Dim lineIndex = algTask.lines.dst1.Get(Of Byte)(pt.Y, pt.X) - 1
            If brickLines(lineIndex) Is Nothing Then
                brickLines(lineIndex) = New List(Of cv.Point)({pt})
            Else
                brickLines(lineIndex).Add(pt)
            End If

            features.Add(pt)
        Next

        dst2 = src.Clone
        For i = 0 To brickLines.Count - 1
            If brickLines(i) Is Nothing Then Continue For
            If brickLines.Count = 1 Then Continue For
            Dim pt = brickLines(i)(0)
            If pt = brickLines(i).Last Then Continue For
            Dim color = vecToScalar(algTask.lines.dst2.Get(Of cv.Vec3b)(pt.Y, pt.X))
            DrawCircle(dst3, pt, color)
            DrawLine(dst2, pt, brickLines(i).Last, color)
            DrawLine(dst3, pt, brickLines(i).Last, color)
        Next
    End Sub
End Class







Public Class Feature_General : Inherits TaskParent
    Public options As New Options_Features
    Public Sub New()
        desc = "Gather features from a list of sources - GoodFeatures, Agast, Brisk..."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If standaloneTest() Then dst2 = algTask.color.Clone

        Dim ptLatest As New List(Of cv.Point2f)
        Dim ptNew As New List(Of cv.Point2f)
        If algTask.optionsChanged = False Then
            For Each pt In algTask.features
                Dim val = algTask.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 0 Then ptNew.Add(pt)
            Next
        End If

        strOut = ""
        Select Case algTask.featureOptions.FeatureMethod.Text
            Case "GoodFeatures"
                ptLatest = cv.Cv2.GoodFeaturesToTrack(algTask.gray, algTask.FeatureSampleSize, options.quality,
                                                      options.minDistance, New cv.Mat,
                                                      options.blockSize, True, options.k).ToList
                strOut = "GoodFeatures produced " + CStr(ptLatest.Count) + " features"
            Case "AGAST"
                If cPtr = 0 Then cPtr = Agast_Open()
                src = algTask.color.Clone
                Dim dataSrc(src.Total * src.ElemSize - 1) As Byte
                Marshal.Copy(src.Data, dataSrc, 0, dataSrc.Length)

                Dim handleSrc = GCHandle.Alloc(dataSrc, GCHandleType.Pinned)
                Dim imagePtr = Agast_Run(cPtr, handleSrc.AddrOfPinnedObject(), src.Rows, src.Cols, options.agastThreshold)
                handleSrc.Free()

                Dim ptMat = cv.Mat.FromPixelData(Agast_Count(cPtr), 1, cv.MatType.CV_32FC2, imagePtr).Clone
                For i = 0 To ptMat.Rows - 1
                    Dim pt = ptMat.Get(Of cv.Point2f)(i, 0)
                    ptLatest.Add(pt)
                    If standaloneTest() Then DrawCircle(dst2, pt, algTask.DotSize, white)
                Next

                strOut = "GoodFeatures produced " + CStr(ptLatest.Count) + " features"
            Case "BRISK"
                Static brisk As New BRISK_Basics
                brisk.Run(algTask.gray)
                ptLatest = brisk.features
                strOut = "GoodFeatures produced " + CStr(ptLatest.Count) + " features"
            Case "Harris"
                Static harris As New Corners_HarrisDetector_CPP
                harris.Run(algTask.gray)
                ptLatest = harris.features
                strOut = "Harris Detector produced " + CStr(ptLatest.Count) + " features"
            Case "FAST"
                Static FAST As New Corners_Basics
                FAST.Run(algTask.gray)
                ptLatest = FAST.features
                strOut = "FAST produced " + CStr(ptLatest.Count) + " features"
            Case "LineInput"
                For Each lp In algTask.lines.lpList
                    ptLatest.Add(lp.ptCenter)
                Next
            Case "BrickPoint"
                Static bPoint As New BrickPoint_Minimum
                bPoint.Run(src)
                For Each pt In bPoint.features
                    ptLatest.Add(pt)
                Next
                strOut = bPoint.labels(2)
        End Select

        algTask.fpFromGridCellLast = New List(Of Integer)(algTask.fpFromGridCell)
        algTask.fpLastList = New List(Of fpData)(algTask.fpList)

        If algTask.optionsChanged Or ptNew.Count = 0 Then
            For Each pt In ptLatest
                ptNew.Add(pt)
            Next
        Else
            For Each pt In ptLatest
                Dim val = algTask.motionMask.Get(Of Byte)(pt.Y, pt.X)
                If val = 255 Then ptNew.Add(pt)
            Next
        End If

        Dim sortByGrid As New SortedList(Of Single, cv.Point2f)(New compareAllowIdenticalSingle)
        For Each pt In ptNew
            Dim index = algTask.gridMap.Get(Of Integer)(pt.Y, pt.X)
            sortByGrid.Add(index, pt)
        Next

        algTask.features.Clear()
        algTask.featurePoints.Clear()
        algTask.fpFromGridCell.Clear()
        For i = 0 To sortByGrid.Count - 1
            Dim pt = sortByGrid.ElementAt(i).Value
            algTask.features.Add(pt)
            algTask.featurePoints.Add(New cv.Point(pt.X, pt.Y))

            Dim nextIndex = algTask.gridMap.Get(Of Integer)(pt.Y, pt.X)
            algTask.fpFromGridCell.Add(nextIndex)
        Next

        If standaloneTest() Then
            For Each pt In algTask.features
                DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
            Next
        End If

        strOut += "  " + CStr(algTask.features.Count) + " features were found using '" + algTask.featureOptions.FeatureMethod.Text +
                  "' method."
        If algTask.heartBeat Then labels(2) = strOut
    End Sub
    Public Sub Close()
        If cPtr <> 0 Then cPtr = Agast_Close(cPtr)
    End Sub
End Class





' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_NoMotionTest : Inherits TaskParent
    Public options As New Options_Features
    Dim method As New Feature_General
    Public Sub New()
        desc = "Find good features to track in a BGR image without using correlation coefficients which produce more consistent sharedResults.images.."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()
        dst2 = src.Clone

        method.Run(src)

        For Each pt In algTask.features
            DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
        Next

        labels(2) = method.labels(2)
    End Sub
End Class







Public Class Feature_Delaunay : Inherits TaskParent
    Dim delaunay As New Delaunay_Contours
    Dim feat As New Feature_Basics
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
            DrawCircle(dst2, pt)
        Next

        delaunay.Run(src)
        dst3 = delaunay.dst2
        For Each pt In delaunay.bPoint.ptList
            DrawCircle(dst3, pt, algTask.DotSize, white)
        Next
        labels(3) = "There were " + CStr(algTask.features.Count) + " Delaunay contours"
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

        If algTask.heartBeat Then dst3.SetTo(0)

        ptList.Clear()
        Dim stationary As Integer, motion As Integer
        For i = 0 To pyr.features.Count - 1
            Dim pt = New cv.Point(pyr.features(i).X, pyr.features(i).Y)
            ptList.Add(pt)
            If ptLast.Contains(pt) Then
                DrawCircle(dst3, pt, algTask.DotSize, algTask.highlight)
                stationary += 1
            Else
                DrawLine(dst3, pyr.lastFeatures(i), pyr.features(i), white)
                motion += 1
            End If
        Next

        If algTask.heartBeat Then labels(3) = CStr(stationary) + " features were stationary and " + CStr(motion) + " features had some motion."
        ptLast = New List(Of cv.Point)(ptList)
    End Sub
End Class









Public Class Feature_Points : Inherits TaskParent
    Dim feat As New Feature_General
    Public Sub New()
        labels(3) = "Features found in the image"
        desc = "Use the sorted list of Delaunay regions to find the top X points to track."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(algTask.grayStable)

        If algTask.heartBeat Then dst2.SetTo(0)

        For Each pt In algTask.features
            DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
        Next
        labels(2) = CStr(algTask.features.Count) + " targets were present with " + CStr(algTask.FeatureSampleSize) + " requested."
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

        If algTask.optionsChanged Then goodList.Clear()

        Dim ptList As New List(Of cv.Point2f)(algTask.features)
        goodList.Add(ptList)

        If goodList.Count >= algTask.frameHistoryCount Then goodList.RemoveAt(0)

        dst2.SetTo(0)
        For Each ptList In goodList
            For Each pt In ptList
                DrawCircle(algTask.color, pt, algTask.DotSize, algTask.highlight)
                Dim c = dst3.Get(Of cv.Vec3b)(pt.Y, pt.X)
                DrawCircle(dst2, pt, algTask.DotSize + 1, c)
            Next
        Next
        labels(2) = CStr(algTask.features.Count) + " features were identified in the image."
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
            dst2 = algTask.leftView
            dst3 = algTask.rightView
            shiTomasi.Run(algTask.leftView)
            dst2.SetTo(cv.Scalar.White, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

            shiTomasi.Run(algTask.rightView)
            dst3.SetTo(algTask.highlight, shiTomasi.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Else
            harris.Run(algTask.leftView)
            dst2 = harris.dst2.Clone
            harris.Run(algTask.rightView)
            dst3 = harris.dst2
        End If
    End Sub
End Class






Public Class Feature_Generations : Inherits TaskParent
    Dim features As New List(Of cv.Point)
    Dim gens As New List(Of Integer)
    Dim feat As New Feature_General
    Public Sub New()
        desc = "Find feature age maximum and average."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(algTask.grayStable)

        Dim newfeatures As New SortedList(Of Integer, cv.Point)(New compareAllowIdenticalIntegerInverted)
        For Each pt In algTask.featurePoints
            Dim index = features.IndexOf(pt)
            If index >= 0 Then newfeatures.Add(gens(index) + 1, pt) Else newfeatures.Add(1, pt)
        Next

        If algTask.heartBeat Then
            features.Clear()
            gens.Clear()
        End If

        features = New List(Of cv.Point)(newfeatures.Values)
        gens = New List(Of Integer)(newfeatures.Keys)

        dst2 = src
        For i = 0 To features.Count - 1
            If gens(i) = 1 Then Exit For
            Dim pt = features(i)
            DrawCircle(dst2, pt, algTask.DotSize, white)
        Next

        If algTask.heartBeat And gens.Count > 0 Then
            labels(2) = CStr(features.Count) + " features found with max/average " + CStr(gens(0)) + "/" + Format(gens.Average, fmt0) + " generations"
        End If
    End Sub
End Class




' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_History : Inherits TaskParent
    Public features As New List(Of cv.Point)
    Dim featureHistory As New List(Of List(Of cv.Point))
    Dim gens As New List(Of Integer)
    Dim feat As New Feature_General
    Public Sub New()
        desc = "Find good features across multiple frames."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(algTask.grayStable)

        dst2 = src.Clone

        featureHistory.Add(New List(Of cv.Point)(algTask.featurePoints))

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

        Dim threshold = If(algTask.frameHistoryCount = 1, 0, 1)
        features.Clear()
        Dim whiteCount As Integer
        For i = 0 To newFeatures.Count - 1
            If gens(i) > threshold Then
                Dim pt = newFeatures(i)
                features.Add(pt)
                If gens(i) < algTask.frameHistoryCount Then
                    DrawCircle(dst2, pt, algTask.DotSize + 2, cv.Scalar.Red)
                Else
                    whiteCount += 1
                    DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
                End If
            End If
        Next

        If featureHistory.Count > algTask.frameHistoryCount Then featureHistory.RemoveAt(0)
        If algTask.heartBeat Then
            labels(2) = CStr(features.Count) + "/" + CStr(whiteCount) + " present/present on every frame" +
                        " Red is a recent addition, yellow is present on previous " +
                        CStr(algTask.frameHistoryCount) + " frames"
        End If
    End Sub
End Class






Public Class Feature_GridPopulation : Inherits TaskParent
    Dim feat As New Feature_General
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        labels(3) = "Click 'Show grid mask overlay' to see grid boundaries."
        desc = "Find the feature population for each cell."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(algTask.grayStable)
        labels(2) = feat.labels(2)

        dst3.SetTo(0)
        For Each pt In algTask.featurePoints
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next

        For Each roi In algTask.gridRects
            Dim test = dst3(roi).FindNonZero()
            SetTrueText(CStr(test.Rows), roi.TopLeft, 3)
        Next
    End Sub
End Class









Public Class Feature_AKaze : Inherits TaskParent
    Dim kazeKeyPoints As cv.KeyPoint() = Nothing
    Dim kaze As cv.AKAZE
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
            DrawCircle(dst2, kazeKeyPoints(i).Pt, algTask.DotSize, algTask.highlight)
        Next
    End Sub
    Public Sub Close()
        If kaze IsNot Nothing Then kaze.Dispose()
    End Sub
End Class





Public Class Feature_RedCloud : Inherits TaskParent
    Dim feat As New Feature_General
    Public Sub New()
        desc = "Show the feature points in the RedCloud output."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)

        dst2 = runRedList(src, labels(2))

        For Each pt In algTask.featurePoints
            DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
        Next
    End Sub
End Class






Public Class Feature_WithDepth : Inherits TaskParent
    Dim feat As New Feature_General
    Public Sub New()
        If algTask.bricks Is Nothing Then algTask.bricks = New Brick_Basics
        desc = "Show the feature points that have depth."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(algTask.grayStable)

        dst2 = src
        Dim depthCount As Integer
        For Each pt In algTask.featurePoints
            Dim index = algTask.gridMap.Get(Of Integer)(pt.Y, pt.X)
            If algTask.bricks.brickList(index).depth > 0 Then
                DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
                depthCount += 1
            End If
        Next
        labels(2) = CStr(depthCount) + " features had depth or " + Format(depthCount / algTask.features.Count, "0%")
    End Sub
End Class





Public Class Feature_Matching : Inherits TaskParent
    Public features As New List(Of cv.Point)
    Public motionPoints As New List(Of cv.Point)
    Dim match As New Match_Basics
    Dim feat As New Feature_General
    Public Sub New()
        algTask.featureOptions.FeatureSampleSize.Value = 150
        desc = "Use correlation coefficient to keep features from frame to frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Static fpLastSrc = src.Clone

        Dim matched As New List(Of cv.Point)
        motionPoints.Clear()
        For Each pt In features
            Dim val = algTask.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val = 0 Then
                Dim index As Integer = algTask.gridMap.Get(Of Integer)(pt.Y, pt.X)
                Dim r = algTask.gridRects(index)
                match.template = fpLastSrc(r)
                match.Run(src(r))
                If match.correlation > algTask.fCorrThreshold Then matched.Add(pt)
            Else
                motionPoints.Add(pt)
            End If
        Next

        labels(2) = "There were " + CStr(features.Count) + " features identified and " + CStr(matched.Count) +
                    " were matched to the previous frame"

        If matched.Count < algTask.FeatureSampleSize / 2 Then
            feat.Run(src)
            features = algTask.featurePoints
        Else
            features = New List(Of cv.Point)(matched)
        End If

        dst2 = src.Clone
        For Each pt In features
            DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
        Next

        fpLastSrc = src.Clone
    End Sub
End Class






Public Class Feature_SteadyCam : Inherits TaskParent
    Public options As New Options_Features
    Dim feat As New Feature_General
    Public Sub New()
        OptionParent.FindSlider("Threshold Percent for Resync").Value = 50
        desc = "Track features using correlation without the motion mask"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        feat.Run(algTask.grayStable)

        Static features As New List(Of cv.Point)(algTask.featurePoints)
        Static lastSrc As cv.Mat = src.Clone

        Dim resync = features.Count / algTask.features.Count < options.resyncThreshold
        If algTask.heartBeat Or algTask.optionsChanged Or resync Then
            features = New List(Of cv.Point)(algTask.featurePoints)
        End If

        Dim ptList = New List(Of cv.Point)(features)
        Dim correlationMat As New cv.Mat
        Dim mode = cv.TemplateMatchModes.CCoeffNormed
        features.Clear()
        For Each pt In ptList
            Dim index As Integer = algTask.gridMap.Get(Of Integer)(pt.Y, pt.X)
            Dim r = algTask.gridRects(index)
            cv.Cv2.MatchTemplate(src(r), lastSrc(r), correlationMat, mode)
            If correlationMat.Get(Of Single)(0, 0) >= algTask.fCorrThreshold Then
                features.Add(pt)
            End If
        Next

        dst2 = src
        For Each pt In features
            DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
        Next

        lastSrc = src.Clone
        labels(2) = CStr(features.Count) + " features were validated by the correlation coefficient"
    End Sub
End Class







Public Class Feature_FacetPoints : Inherits TaskParent
    Dim delaunay As New Delaunay_Basics
    Dim feat As New Feature_General
    Public Sub New()
        desc = "Assign each delaunay point to a RedCell"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(src)
        dst2 = runRedList(src, labels(2))

        delaunay.inputPoints = algTask.features
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
            Dim index = algTask.redList.rcMap.Get(Of Byte)(pt.Y, pt.X)
            If index = 0 Then Continue For
            Dim rc = algTask.redList.oldrclist(index)
            Dim val = algTask.pcSplit(2).Get(Of Single)(pt.Y, pt.X)
            If val <> 0 Then
                rc.ptFacets.Add(pt)
                algTask.redList.oldrclist(index) = rc
            End If
        Next

        For Each rc In algTask.redList.oldrclist
            For Each pt In rc.ptFacets
                DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
            Next
        Next

        If standalone And algTask.redList.oldrclist.Count > 0 Then
            algTask.color.Rectangle(algTask.oldrcD.rect, algTask.highlight, algTask.lineWidth)
            For Each pt In algTask.oldrcD.ptFacets
                DrawCircle(algTask.color, pt, algTask.DotSize, algTask.highlight)
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

        If algTask.optionsChanged Then
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
            Dim val = algTask.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val = 0 Then newList.Add(pt)
        Next

        For Each pt In currPoints
            Dim val = algTask.motionMask.Get(Of Byte)(pt.Y, pt.X)
            If val <> 0 Then newList.Add(pt)
        Next

        stablePoints = New List(Of cv.Point2f)(newList)
        dst2 = src
        For Each pt In stablePoints
            DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
        Next
        labels(2) = $"Found {keypoints.Length} features with agast"
    End Sub
    Public Sub Close()
        If agastFD IsNot Nothing Then agastFD.Dispose()
    End Sub
End Class






Public Class Feature_NoMotion : Inherits TaskParent
    Dim feat As New Feature_General
    Public Sub New()
        algTask.gOptions.UseMotionMask.Checked = False
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Find good features to track in a BGR image using the motion mask+"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(algTask.grayStable)
        dst2 = src.Clone

        dst3.SetTo(0)
        For Each pt In algTask.featurePoints
            DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
            dst3.Set(Of Byte)(pt.Y, pt.X, 255)
        Next

        labels(2) = feat.labels(2)
    End Sub
End Class





Public Class Feature_StableVisual : Inherits TaskParent
    Dim noMotion As New Feature_General
    Public fpStable As New List(Of fpData)
    Public ptStable As New List(Of cv.Point)
    Public Sub New()
        desc = "Show only features present on this and the previous frame."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        Dim lastFeatures As New List(Of cv.Point)(algTask.featurePoints)

        noMotion.Run(src)
        dst3 = noMotion.dst2

        dst2.SetTo(0)
        Dim stable As New List(Of cv.Point)
        For Each pt In algTask.featurePoints
            If lastFeatures.Contains(pt) Then
                DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
                stable.Add(pt)
            End If
        Next
        lastFeatures = New List(Of cv.Point)(stable)
        labels(2) = noMotion.labels(2) + " and " + CStr(stable.Count) + " appeared on earlier frames "

        dst2 = src.Clone
        For Each pt In stable
            DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
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
        Dim lastFeatures As New List(Of cv.Point)(algTask.featurePoints)

        noMotion.Run(src)

        dst2 = src
        Dim stable As New List(Of cv.Point)
        For Each pt In algTask.featurePoints
            If lastFeatures.Contains(pt) Then
                DrawCircle(dst2, pt, algTask.DotSize, algTask.highlight)
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
                DrawCircle(dst3, fp.pt, algTask.DotSize, algTask.highlight)
                SetTrueText(CStr(fp.age), fp.pt, 3)
            End If
        Next
    End Sub
End Class








' https://docs.opencv.org/3.4/d7/d8b/tutorial_py_lucas_kanade.html
Public Class Feature_KNN : Inherits TaskParent
    Dim knn As New KNN_Basics
    Public featurePoints As New List(Of cv.Point2f)
    Dim feat As New Feature_General
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Find good features to track in the image but use the same point if closer than a threshold"
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        feat.Run(algTask.grayStable)

        If algTask.features.Count = 0 Then
            featurePoints.Clear()
            Exit Sub
        End If

        knn.queries = New List(Of cv.Point2f)(algTask.features)
        If algTask.firstPass Or algTask.gOptions.DebugCheckBox.Checked Then
            knn.trainInput = New List(Of cv.Point2f)(knn.queries)
            algTask.gOptions.DebugCheckBox.Checked = False
        End If

        knn.Run(src)

        For i = 0 To knn.neighbors.Count - 1
            Dim trainIndex = knn.neighbors(i)(0) ' index of the matched train input
            Dim pt = knn.trainInput(trainIndex)
            Dim qPt = algTask.features(i)
            If pt.DistanceTo(qPt) > 2 Then knn.trainInput(trainIndex) = algTask.features(i)
        Next

        featurePoints = New List(Of cv.Point2f)(knn.trainInput)

        src.CopyTo(dst2)
        dst3.SetTo(0)
        For Each pt In featurePoints
            DrawCircle(dst2, pt, algTask.DotSize + 2, white)
            DrawCircle(dst3, pt, algTask.DotSize + 2, white)
        Next

        labels(2) = feat.labels(2)
        labels(3) = feat.labels(2)
    End Sub
End Class
