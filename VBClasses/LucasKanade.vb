Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    ' https://www.learnopencvb.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
    Public Class LucasKanade_Basics : Inherits TaskParent
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
            For Each pt In feat.ptlist
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
                        Circle(dst3, pt2, task.DotSize + 1, Scalar.Red, -1, task.lineType)
                    End If
                End If
            Next
            labels(2) = "Matched " + CStr(features.Count) + " points "

            If heartbeatX() Then lastGray = src.Clone()
            lastGray = src.Clone()
        End Sub
    End Class




    Public Class XR_LucasKanade_Basics : Inherits TaskParent
        Dim pyr As New LucasKanade_Basics
        Public ptList As New List(Of cv.Point)
        Public ptLast As New List(Of cv.Point)
        Public Sub New()
            desc = "Provide a trace of the tracked features"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            pyr.Run(src)
            dst2 = pyr.dst2
            labels(2) = pyr.labels(2)

            If heartbeatX() Then dst3.SetTo(0)

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
End Namespace