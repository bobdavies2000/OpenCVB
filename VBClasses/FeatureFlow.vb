Imports cv = OpenCvSharp
Public Class FeatureFlow_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim feat As New Feature_Basics
    Public Sub New()
        desc = "Use correlations to confirm that points match the previous frame."
    End Sub
    Public Sub buildCorrelations(prevFeatures As List(Of cv.Point), currFeatures As List(Of cv.Point))
        Dim correlationmat As New cv.Mat
        lpList.Clear()
        Dim pad = task.brickEdgeLen / 2
        For Each p1 In prevFeatures
            Dim rect = ValidateRect(New cv.Rect(p1.X - pad, p1.Y - pad, task.brickEdgeLen, task.brickEdgeLen))
            Dim correlations As New List(Of Single)
            For Each p2 In currFeatures
                Dim r = ValidateRect(New cv.Rect(p2.X - pad, p2.Y - pad, Math.Min(rect.Width, task.brickEdgeLen),
                                                                                 Math.Min(task.brickEdgeLen, rect.Height)))
                cv.Cv2.MatchTemplate(dst2(rect), dst3(r), correlationmat, cv.TemplateMatchModes.CCoeffNormed)
                correlations.Add(correlationmat.Get(Of Single)(0, 0))
            Next
            Dim maxCorrelation = correlations.Max
            If maxCorrelation >= task.fCorrThreshold Then
                Dim index = correlations.IndexOf(maxCorrelation)
                lpList.Add(New lpData(p1, currFeatures(index)))
            End If
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then dst1 = task.gray.Clone Else dst1 = src.Clone
        feat.Run(dst1)

        labels = feat.labels

        dst2 = task.color.Clone
        Static prevFeatures As New List(Of cv.Point)(task.featurePoints)
        buildCorrelations(prevFeatures, task.featurePoints)

        For Each pt In task.featurePoints
            DrawCircle(dst2, pt, task.DotSize, task.highlight)
        Next
        prevFeatures = New List(Of cv.Point)(task.featurePoints)
    End Sub
End Class





' https://www.learnopencvb.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
Public Class FeatureFlow_LucasKanade : Inherits TaskParent
    Public features As New List(Of cv.Point2f)
    Public lastFeatures As New List(Of cv.Point2f)
    Dim options As New Options_OpticalFlowSparse
    Dim feat As New Feature_BasicsNew
    Public Sub New()
        desc = "Show the optical flow of a sparse matrix."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        options.Run()

        If src.Channels <> 1 Then src = task.gray
        feat.Run(src)

        dst2 = src.Clone()
        dst3 = src.Clone()

        Static lastGray As cv.Mat = task.gray.Clone
        features = task.features
        Dim features1 = cv.Mat.FromPixelData(features.Count, 1, cv.MatType.CV_32FC2, features.ToArray)
        Dim features2 = New cv.Mat
        Dim status As New cv.Mat, err As New cv.Mat, winSize As New cv.Size(3, 3)
        cv.Cv2.CalcOpticalFlowPyrLK(src, lastGray, features1, features2, status, err, winSize, 3, term, options.OpticalFlowFlag)
        features = New List(Of cv.Point2f)
        lastFeatures.Clear()
        For i = 0 To status.Rows - 1
            If status.Get(Of Byte)(i, 0) Then
                Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If length < 30 Then
                    features.Add(pt1)
                    lastFeatures.Add(pt2)
                    dst2.Line(pt1, pt2, task.highlight, task.lineWidth + task.lineWidth, task.lineType)
                    DrawCircle(dst3, pt1, task.DotSize + 3, white)
                    DrawCircle(dst3, pt2, task.DotSize + 1, cv.Scalar.Red)
                End If
            End If
        Next
        labels(2) = "Matched " + CStr(features.Count) + " points "

        If task.heartBeat Then lastGray = src.Clone()
        lastGray = src.Clone()
    End Sub
End Class