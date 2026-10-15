Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class FeatureFlow_Basics : Inherits TaskParent
        Dim feat As New Feature_Basics
        Public ptListConfirmed As New List(Of cv.Point)
        Public plot As New PlotTime_Basics
        Public Sub New()
            plot.maxScale = 50
            plot.minScale = 0
            plot.plotCount = 1
            labels(3) = "Plot of the number of confirmed features over time..."
            desc = "Use correlations to confirm that points match the previous frame."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then dst1 = task.gray.Clone Else dst1 = src.Clone
            feat.Run(dst1)

            dst2 = task.color.Clone
            Static prevFeatures As New List(Of cv.Point)(feat.ptlist)
            Dim correlationmat As New Mat
            Dim pad = task.gridWH
            ptListConfirmed.Clear()
            For Each p1 In prevFeatures
                Dim rect = ValidateRect(New cv.Rect(p1.X - pad, p1.Y - pad,
                                                    task.gridWH * 2, task.gridWH * 2))
                Dim correlations As New List(Of Single)
                For Each p2 In feat.ptlist
                    Dim r = ValidateRect(New cv.Rect(p2.X - pad, p2.Y - pad,
                                         Math.Min(rect.Width, task.gridWH * 2),
                                         Math.Min(rect.Height, task.gridWH * 2)))
                    MatchTemplate(dst2(rect), dst0(r), correlationmat, TemplateMatchModes.CCoeffNormed)
                    correlations.Add(correlationmat.Get(Of Single)(0, 0))
                Next
                Dim maxCorrelation = correlations.Max
                If maxCorrelation >= task.fCorrThreshold Then
                    Dim index = correlations.IndexOf(maxCorrelation)
                    Circle(dst2, p1, task.DotSize, task.highlight, -1, task.lineType)
                    SetTrueText(maxCorrelation.ToString("F3"), p1, 2)
                    ptListConfirmed.Add(p1)
                End If
            Next

            plot.plotData = New Scalar(ptListConfirmed.Count)
            plot.Run(src)
            dst3 = plot.dst2

            prevFeatures = New List(Of cv.Point)(feat.ptlist)
            dst0 = task.color.Clone
            labels(2) = CStr(ptListConfirmed.Count) + " points were confirmed (matched to the previous frame)"
        End Sub
    End Class





    ' https://www.learnopencvb.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
    Public Class FeatureFlow_LucasKanade : Inherits TaskParent
        Public features As New List(Of Point2f)
        Public lastFeatures As New List(Of Point2f)
        Dim options As New Options_OpticalFlowSparse
        Dim feat As New Feature_Basics
        Public Sub New()
            desc = "Show the optical flow of a sparse matrix."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            If src.Channels <> 1 Then src = task.gray
            feat.Run(src)

            CvtColor(src, dst2, ColorConversionCodes.GRAY2BGR)
            CvtColor(src, dst3, ColorConversionCodes.GRAY2BGR)

            Static lastGray As Mat = task.gray.Clone
            features.Clear()
            For Each pt In feat.ptList
                features.Add(pt)
            Next
            Dim features1 = Mat.FromPixelData(features.Count, 1, MatType.CV_32FC2, features.ToArray)
            Dim features2 = New Mat
            Dim status As New Mat, err As New Mat, winSize As New Size(3, 3)
            CalcOpticalFlowPyrLK(src, lastGray, features1, features2, status, err, winSize, 3, task.term, options.OpticalFlowFlag)
            features = New List(Of Point2f)
            lastFeatures.Clear()
            For i = 0 To status.Rows - 1
                If status.Get(Of Byte)(i, 0) Then
                    Dim pt1 = features1.Get(Of Point2f)(i, 0)
                    Dim pt2 = features2.Get(Of Point2f)(i, 0)
                    Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                    If length < 30 Then
                        features.Add(pt1)
                        lastFeatures.Add(pt2)
                        Line(dst2, pt1, pt2, task.highlight, task.lineWidth + task.lineWidth, task.lineType)
                        Circle(dst3, pt1, task.DotSize + 3, white, -1, task.lineType)
                        Circle(dst3, pt2, task.DotSize + 1, Scalar.red, -1, task.lineType)
                    End If
                End If
            Next
            labels(2) = "Matched " + CStr(features.Count) + " points "

            if heartbeatx() then  lastGray = src.Clone()
            lastGray = src.Clone()
        End Sub
    End Class
End Namespace