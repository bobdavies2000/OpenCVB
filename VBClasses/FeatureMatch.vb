Imports OpenCvSharp : Imports OpenCvSharp.Cv2 : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FeatureMatch_Basics : Inherits TaskParent
        Implements IDisposable
        Dim akaze As XFeatures2D.AKAZE
        Dim matcher As BFMatcher
        Dim lastFrame As Mat
        Dim lastKeyPoints As KeyPoint()
        Dim lastDesc As New Mat
        Public ptList As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            akaze = XFeatures2D.AKAZE.Create()
            matcher = New BFMatcher(NormTypes.Hamming, crossCheck:=False)
            labels = {"", "", "Current AKAZE keypoints", "Matches connected to previous frame"}
            desc = "Cursor.ai: Detect AKAZE keypoints and match them to the next camera frame."
        End Sub
        Public Shared Sub DisplayMatches(dst As cv.Mat, good As List(Of DMatch), lastKeyPoints As KeyPoint(),
                                     keypoints As KeyPoint(), features As List(Of cv.Point),
                                     lastFeatures As List(Of cv.Point))
            features.Clear()
            lastFeatures.Clear()
            For Each m In good
                Dim p0 = lastKeyPoints(m.QueryIdx).Pt
                Dim p1 = keypoints(m.TrainIdx).Pt
                lastFeatures.Add(p0)
                features.Add(p1)
                Line(dst, p0, p1, task.highlight, task.lineWidth, task.lineType)
                Circle(dst, p0, task.DotSize, task.highlight, -1, task.lineType)
                Circle(dst, p1, task.DotSize + 1, Scalar.red, -1, task.lineType)
            Next
        End Sub
        Public Shared Sub captureState(ByRef lastframe As cv.Mat, ByRef lastdesc As cv.Mat, ByRef descMat As cv.Mat,
                                   ByRef lastKeyPoints As KeyPoint(), ByRef keypoints As KeyPoint())
            lastframe = task.color.Clone
            lastKeyPoints = keypoints
            lastdesc = descMat.Clone()
        End Sub
        Public Shared Function getMatches(knn As Object) As List(Of DMatch)
            Dim good As New List(Of DMatch)
            For Each pair In knn
                If pair IsNot Nothing AndAlso pair.Length >= 2 Then
                    If pair(0).Distance < 0.75F * pair(1).Distance Then good.Add(pair(0))
                End If
            Next
            Return good
        End Function
        Public Shared Function displayMatches(keypoints As KeyPoint()) As cv.Mat
            Dim dst = task.color.Clone
            If keypoints IsNot Nothing Then
                For Each kp In keypoints
                    Circle(dst, kp.Pt, task.DotSize, task.highlight, -1, task.lineType)
                Next
            End If
            Return dst
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim color = If(src.Channels() = 1, task.color.Clone, src.Clone)
            Dim gray = If(src.Channels() = 1, src.Clone, task.gray.Clone)

            Dim keyPoints = akaze.Detect(gray)
            Dim maxFeat = task.fOptions.FeatureSizeSlider.Value
            If keyPoints IsNot Nothing AndAlso keyPoints.Length > maxFeat Then
                keyPoints = KeyPointsFilter.RetainBest(keyPoints, maxFeat)
            End If

            Dim descMat As New Mat()
            If keyPoints IsNot Nothing AndAlso keyPoints.Length > 0 Then
                akaze.Compute(gray, keyPoints, descMat)
            End If

            dst2 = FeatureMatch_Basics.displayMatches(keyPoints)

            ptList.Clear()
            lastFeatures.Clear()
            If Not lastDesc.Empty() AndAlso Not descMat.Empty() AndAlso
               lastKeyPoints IsNot Nothing AndAlso lastKeyPoints.Length > 0 AndAlso
               keyPoints IsNot Nothing AndAlso keyPoints.Length > 0 Then

                Dim knn = matcher.KnnMatch(lastDesc, descMat, k:=2)
                Dim matches = FeatureMatch_Basics.getMatches(knn)

                dst3 = lastFrame.Clone
                FeatureMatch_Basics.DisplayMatches(dst3, matches, lastKeyPoints, keyPoints, ptList, lastFeatures)

                labels(2) = CStr(If(keyPoints Is Nothing, 0, keyPoints.Length)) + " AKAZE keypoints (max " +
                            CStr(maxFeat) + ") on current frame"
                labels(3) = (matches.Count / lastKeyPoints.Length).ToString("0%") + " matched to previous frame."
            End If

            FeatureMatch_Basics.captureState(lastFrame, lastDesc, descMat, lastKeyPoints, keyPoints)
        End Sub
        Protected Overrides Sub Finalize()
            If akaze IsNot Nothing Then akaze.Dispose()
            If matcher IsNot Nothing Then matcher.Dispose()
            If lastDesc IsNot Nothing Then lastDesc.Dispose()
        End Sub
    End Class





    Public Class FeatureMatch_BRISK : Inherits TaskParent
        Implements IDisposable
        Dim brisk As XFeatures2D.BRISK
        Dim matcher As BFMatcher
        Dim lastFrame As Mat
        Dim lastKeyPoints As KeyPoint()
        Dim lastDesc As New Mat
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            brisk = XFeatures2D.BRISK.Create()
            matcher = New BFMatcher(NormTypes.Hamming, crossCheck:=False)
            desc = "Cursor.ai: Detect BRISK keypoints and match them to the next camera frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim keyPoints As KeyPoint() = Nothing
            Dim descMat As New Mat()
            brisk.DetectAndCompute(task.gray, Nothing, keyPoints, descMat)

            dst2 = FeatureMatch_Basics.displayMatches(keyPoints)

            If Not lastDesc.Empty() AndAlso Not descMat.Empty() AndAlso
           lastKeyPoints IsNot Nothing AndAlso lastKeyPoints.Length > 0 AndAlso
           keyPoints IsNot Nothing AndAlso keyPoints.Length > 0 Then

                Dim knn = matcher.KnnMatch(lastDesc, descMat, k:=2)
                Dim matches = FeatureMatch_Basics.getMatches(knn)

                dst3 = lastFrame.Clone
                FeatureMatch_Basics.DisplayMatches(dst3, matches, lastKeyPoints, keyPoints, features, lastFeatures)

                labels(2) = CStr(If(keyPoints Is Nothing, 0, keyPoints.Length)) + " BRISK keypoints on current frame"
                labels(3) = (matches.Count / lastKeyPoints.Length).ToString("0%") + " matched to previous frame."
            End If

            if heartbeatx() then  FeatureMatch_Basics.captureState(lastFrame, lastDesc, descMat, lastKeyPoints, keyPoints)
        End Sub
        Protected Overrides Sub Finalize()
            If brisk IsNot Nothing Then brisk.Dispose()
            If matcher IsNot Nothing Then matcher.Dispose()
            If lastDesc IsNot Nothing Then lastDesc.Dispose()
        End Sub
    End Class






    Public Class FeatureMatch_SIFT : Inherits TaskParent
        Implements IDisposable
        Dim sift As SIFT
        Dim matcher As BFMatcher
        Dim lastFrame As Mat
        Dim lastKeyPoints As KeyPoint()
        Dim lastDesc As New Mat
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            sift = SIFT.Create()
            matcher = New BFMatcher(NormTypes.L2, crossCheck:=False)
            desc = "Cursor.ai: Detect SIFT keypoints and match them to the next camera frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim keyPoints As KeyPoint() = Nothing
            Dim descMat As New Mat()
            sift.DetectAndCompute(task.gray, Nothing, keyPoints, descMat)

            dst2 = FeatureMatch_Basics.displayMatches(keyPoints)

            If Not lastDesc.Empty() AndAlso Not descMat.Empty() AndAlso
           lastKeyPoints IsNot Nothing AndAlso lastKeyPoints.Length > 0 AndAlso
           keyPoints IsNot Nothing AndAlso keyPoints.Length > 0 Then

                Dim knn = matcher.KnnMatch(lastDesc, descMat, k:=2)
                Dim matches = FeatureMatch_Basics.getMatches(knn)

                dst3 = lastFrame.Clone
                FeatureMatch_Basics.DisplayMatches(dst3, matches, lastKeyPoints, keyPoints, features, lastFeatures)

                labels(2) = CStr(If(keyPoints Is Nothing, 0, keyPoints.Length)) + " SIFT keypoints on current frame"
                labels(3) = (matches.Count / lastKeyPoints.Length).ToString("0%") + " matched to previous frame."
            End If

            if heartbeatx() then  FeatureMatch_Basics.captureState(lastFrame, lastDesc, descMat, lastKeyPoints, keyPoints)
        End Sub
        Protected Overrides Sub Finalize()
            If sift IsNot Nothing Then sift.Dispose()
            If matcher IsNot Nothing Then matcher.Dispose()
            If lastDesc IsNot Nothing Then lastDesc.Dispose()
        End Sub
    End Class






    Public Class FeatureMatch_SURF : Inherits TaskParent
        Implements IDisposable
        Dim surf As XFeatures2D.SURF
        Dim matcher As BFMatcher
        Dim lastFrame As Mat
        Dim lastKeyPoints As KeyPoint()
        Dim lastDesc As New Mat
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            surf = XFeatures2D.SURF.Create(2000)
            matcher = New BFMatcher(NormTypes.L2, crossCheck:=False)
            desc = "Cursor.ai: Detect SURF keypoints and match them to the next camera frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim keyPoints As KeyPoint() = Nothing
            Dim descMat As New Mat()
            surf.DetectAndCompute(task.gray, Nothing, keyPoints, descMat)

            dst2 = FeatureMatch_Basics.displayMatches(keyPoints)

            If Not lastDesc.Empty() AndAlso Not descMat.Empty() AndAlso
           lastKeyPoints IsNot Nothing AndAlso lastKeyPoints.Length > 0 AndAlso
           keyPoints IsNot Nothing AndAlso keyPoints.Length > 0 Then

                Dim knn = matcher.KnnMatch(lastDesc, descMat, k:=2)
                Dim matches = FeatureMatch_Basics.getMatches(knn)

                dst3 = lastFrame.Clone
                FeatureMatch_Basics.DisplayMatches(dst3, matches, lastKeyPoints, keyPoints, features, lastFeatures)

                labels(2) = CStr(If(keyPoints Is Nothing, 0, keyPoints.Length)) + " SURF keypoints on current frame"
                labels(3) = (matches.Count / lastKeyPoints.Length).ToString("0%") + " matched to previous frame."
            End If

            if heartbeatx() then  FeatureMatch_Basics.captureState(lastFrame, lastDesc, descMat, lastKeyPoints, keyPoints)
        End Sub
        Protected Overrides Sub Finalize()
            If surf IsNot Nothing Then surf.Dispose()
            If matcher IsNot Nothing Then matcher.Dispose()
            If lastDesc IsNot Nothing Then lastDesc.Dispose()
        End Sub
    End Class






    Public Class FeatureMatch_KAZE : Inherits TaskParent
        Implements IDisposable
        Dim kaze As XFeatures2D.KAZE
        Dim matcher As BFMatcher
        Dim lastFrame As Mat
        Dim lastKeyPoints As KeyPoint()
        Dim lastDesc As New Mat
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            kaze = XFeatures2D.KAZE.Create()
            matcher = New BFMatcher(NormTypes.L2, crossCheck:=False)
            desc = "Cursor.ai: Detect KAZE keypoints and match them to the next camera frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim keyPoints As KeyPoint() = Nothing
            Dim descMat As New Mat()
            kaze.DetectAndCompute(task.gray, Nothing, keyPoints, descMat)

            dst2 = FeatureMatch_Basics.displayMatches(keyPoints)

            If Not lastDesc.Empty() AndAlso Not descMat.Empty() AndAlso
           lastKeyPoints IsNot Nothing AndAlso lastKeyPoints.Length > 0 AndAlso
           keyPoints IsNot Nothing AndAlso keyPoints.Length > 0 Then

                Dim knn = matcher.KnnMatch(lastDesc, descMat, k:=2)
                Dim matches = FeatureMatch_Basics.getMatches(knn)

                dst3 = lastFrame.Clone
                FeatureMatch_Basics.DisplayMatches(dst3, matches, lastKeyPoints, keyPoints, features, lastFeatures)

                labels(2) = CStr(If(keyPoints Is Nothing, 0, keyPoints.Length)) + " KAZE keypoints on current frame"
                labels(3) = (matches.Count / lastKeyPoints.Length).ToString("0%") + " matched to previous frame."
            End If

            if heartbeatx() then  FeatureMatch_Basics.captureState(lastFrame, lastDesc, descMat, lastKeyPoints, keyPoints)
        End Sub
        Protected Overrides Sub Finalize()
            If kaze IsNot Nothing Then kaze.Dispose()
            If matcher IsNot Nothing Then matcher.Dispose()
            If lastDesc IsNot Nothing Then lastDesc.Dispose()
        End Sub
    End Class





    Public Class FeatureMatch_ORB : Inherits TaskParent
        Implements IDisposable
        Dim orb As ORB
        Dim matcher As BFMatcher
        Dim lastFrame As Mat
        Dim lastKeyPoints As KeyPoint()
        Dim lastDesc As New Mat
        Public features As New List(Of cv.Point)
        Public lastFeatures As New List(Of cv.Point)
        Public Sub New()
            matcher = New BFMatcher(NormTypes.Hamming2, crossCheck:=False)
            desc = "Cursor.ai: Detect ORB keypoints and match them to the next camera frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If orb Is Nothing OrElse task.optionsChanged Then
                If orb IsNot Nothing Then orb.Dispose()
                orb = ORB.Create(task.fOptions.FeatureSizeSlider.Value)
            End If

            Dim keyPoints As KeyPoint() = Nothing
            Dim descMat As New Mat()
            orb.DetectAndCompute(task.gray, Nothing, keyPoints, descMat)

            dst2 = FeatureMatch_Basics.displayMatches(keyPoints)

            If Not lastDesc.Empty() AndAlso Not descMat.Empty() AndAlso
           lastKeyPoints IsNot Nothing AndAlso lastKeyPoints.Length > 0 AndAlso
           keyPoints IsNot Nothing AndAlso keyPoints.Length > 0 Then

                Dim knn = matcher.KnnMatch(lastDesc, descMat, k:=2)
                Dim matches = FeatureMatch_Basics.getMatches(knn)

                dst3 = lastFrame.Clone
                FeatureMatch_Basics.DisplayMatches(dst3, matches, lastKeyPoints, keyPoints, features, lastFeatures)

                labels(2) = CStr(If(keyPoints Is Nothing, 0, keyPoints.Length)) + " ORB keypoints on current frame"
                labels(3) = (matches.Count / lastKeyPoints.Length).ToString("0%") + " matched to previous frame."
            End If

            if heartbeatx() then  FeatureMatch_Basics.captureState(lastFrame, lastDesc, descMat, lastKeyPoints, keyPoints)
        End Sub
        Protected Overrides Sub Finalize()
            If orb IsNot Nothing Then orb.Dispose()
            If matcher IsNot Nothing Then matcher.Dispose()
            If lastDesc IsNot Nothing Then lastDesc.Dispose()
        End Sub
    End Class






    Public Class FeatureMatch_Tracker : Inherits TaskParent
        Dim feat As New FeatureMatch_Basics
        Const maxSamples As Integer = 30
        Public tracks As New List(Of List(Of cv.Point))
        Public Sub New()
            labels = {"", "", "AKAZE features", "Feature tracks (up to 30 samples; dropped on lost track)"}
            desc = "Cursor.ai: Track FeatureMatch_Basics features for up to 30 samples; drop a track and its history when matching is lost."
        End Sub
        Private Shared Function findPairIndex(tip As cv.Point, lastFeatures As List(Of cv.Point), used() As Boolean) As Integer
            Dim best = -1
            Dim bestDist = Double.MaxValue
            For i = 0 To lastFeatures.Count - 1
                If used(i) Then Continue For
                Dim d = tip.DistanceTo(lastFeatures(i))
                If d < bestDist Then
                    bestDist = d
                    best = i
                End If
            Next
            If best >= 0 AndAlso bestDist <= 2 Then Return best
            Return -1
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.optionsChanged Then tracks.Clear()

            feat.Run(src)
            dst2 = feat.dst2
            labels(2) = feat.labels(2)

            Dim newTracks As New List(Of List(Of cv.Point))
            If feat.lastFeatures.Count > 0 AndAlso feat.ptList.Count = feat.lastFeatures.Count Then
                Dim used(feat.lastFeatures.Count - 1) As Boolean

                For Each track In tracks
                    Dim idx = findPairIndex(track(track.Count - 1), feat.lastFeatures, used)
                    If idx < 0 Then Continue For ' lost track — drop history

                    used(idx) = True
                    track.Add(feat.ptList(idx))
                    If track.Count > maxSamples Then track.RemoveAt(1) ' keep track(0) as the origin
                    newTracks.Add(track)
                Next

                For i = 0 To feat.lastFeatures.Count - 1
                    If used(i) Then Continue For
                    Dim track As New List(Of cv.Point)({feat.lastFeatures(i), feat.ptList(i)})
                    newTracks.Add(track)
                Next
            End If
            tracks = newTracks

            dst3 = task.color.Clone
            Dim longTracks As Integer
            Dim averageDistance As Double
            For Each track In tracks
                Dim color = task.scalarColors(track.Count Mod 255)
                For j = 1 To track.Count - 1
                    Line(dst3, track(j - 1), track(j), color, task.lineWidth, task.lineType)
                Next
                Circle(dst3, track(track.Count - 1), task.DotSize + 1, color, -1, task.lineType)
                If track.Count >= maxSamples Then longTracks += 1
                averageDistance += track(0).DistanceTo(track(track.Count - 1))
            Next
            If tracks.Count > 0 Then averageDistance /= tracks.Count

            labels(3) = CStr(tracks.Count) + " tracks, " + CStr(longTracks) + " at " + CStr(maxSamples) +
                    " samples, avg start-to-end = " + averageDistance.ToString(fmt1) + " px"
        End Sub
    End Class




    Public Class Feature_PointsPath : Inherits TaskParent
        Dim feat As New FeatureMatch_Basics
        Public Sub New()
            labels(3) = "Features found in the image"
            desc = "Use the sorted list of Delaunay regions to find the top X points to track."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            feat.Run(task.gray)

            if heartbeatx() then  dst2.SetTo(0)

            For Each pt In feat.ptList
                Circle(dst2, pt, task.DotSize, task.highlight, -1, task.lineType)
            Next
            labels(2) = CStr(feat.ptList.Count) + " targets were present with " + CStr(task.fOptions.FeatureSizeSlider.Value) + " requested."
        End Sub
    End Class
End Namespace