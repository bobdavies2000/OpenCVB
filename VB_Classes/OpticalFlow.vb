Imports cv = OpenCvSharp
'https://www.learnopencv.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
Public Class OpticalFlow_DenseBasics : Inherits VB_Algorithm
    Dim options As New Options_OpticalFlow
    Public Sub New()
        desc = "Use dense optical flow algorithm  "
    End Sub
    Public Sub RunVB(src as cv.Mat)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Options.RunVB()
        Static lastGray As cv.Mat = src.Clone
        Dim hsv = opticalFlow_Dense(lastGray, src, options.pyrScale, options.levels, options.winSize, options.iterations, options.polyN,
                                    options.polySigma, options.OpticalFlowFlags)

        dst2 = hsv.CvtColor(cv.ColorConversionCodes.HSV2RGB)
        dst2 = dst2.ConvertScaleAbs(options.outputScaling)
        dst3 = dst2.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        lastGray = src.Clone()
    End Sub
End Class







' https://www.learnopencv.com/optical-flow-in-opencv/?ck_subscriber_id=785741175
Public Class OpticalFlow_Sparse : Inherits VB_Algorithm
    Public features As New List(Of cv.Point2f)
    Dim feat As New Feature_Basics
    Dim sumScale As cv.Mat, sScale As cv.Mat
    Dim errScale As cv.Mat, qScale As cv.Mat, rScale As cv.Mat
    Dim options As New Options_OpticalFlowSparse
    Public Sub New()
        desc = "Show the optical flow of a sparse matrix."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        dst2 = src.Clone()
        dst3 = src.Clone()

        If task.optionsChanged Then
            errScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 1)
            qScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.004)
            rScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0.5)
            sumScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
            sScale = New cv.Mat(5, 1, cv.MatType.CV_64F, 0)
        End If

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static lastGray As cv.Mat = src.Clone
        feat.Run(src)
        features = task.features
        Dim features1 = New cv.Mat(features.Count, 1, cv.MatType.CV_32FC2, features.ToArray)
        Dim features2 = New cv.Mat
        Dim status As New cv.Mat, err As New cv.Mat, winSize As New cv.Size(3, 3)
        cv.Cv2.CalcOpticalFlowPyrLK(src, lastgray, features1, features2, status, err, winSize, 3, term, options.OpticalFlowFlag)
        features = New List(Of cv.Point2f)
        Dim lastFeatures As New List(Of cv.Point2f)
        For i = 0 To status.Rows - 1
            If status.Get(Of Byte)(i, 0) Then
                Dim pt1 = features1.Get(Of cv.Point2f)(i, 0)
                Dim pt2 = features2.Get(Of cv.Point2f)(i, 0)
                Dim length = Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) + (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y))
                If length < 30 Then
                    features.Add(pt1)
                    lastFeatures.Add(pt2)
                    dst2.Line(pt1, pt2, cv.Scalar.Red, task.lineWidth + task.lineWidth + 2, task.lineType)
                    dst3.Circle(pt1, task.dotSize + 3, cv.Scalar.White, -1, task.lineType)
                    dst3.Circle(pt2, task.dotSize + 1, cv.Scalar.Red, -1, task.lineType)
                End If
            End If
        Next
        labels(2) = "Matched " + CStr(features.Count) + " points "

        If task.heartBeat Then lastGray = src.Clone()
        lastGray = src.Clone()
    End Sub
End Class