Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/lkdemo.cpp
Public Class KLT_Basics : Inherits VB_Parent
    Public status As New cv.Mat
    Public outputMat As New cv.Mat
    Public circleColor = cv.Scalar.Red
    Public options As New Options_KLT
    Public Sub New()
        term = New cv.TermCriteria(cv.CriteriaTypes.Eps Or cv.CriteriaTypes.Count, 10, 1.0)
        desc = "Track movement with Kanada-Lucas-Tomasi algorithm"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Options.RunVB()

        If options.nightMode Then dst2.SetTo(0) Else src.CopyTo(dst2)
        Static lastGray As cv.Mat = src.Clone

        If src.Channels() = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)
        If options.inputPoints Is Nothing Then
            options.inputPoints = cv.Cv2.GoodFeaturesToTrack(src, options.maxCorners, options.qualityLevel,
                                                             options.minDistance, New cv.Mat, options.blockSize, False, 0)
            If options.inputPoints.Length > 0 Then
                options.inputPoints = cv.Cv2.CornerSubPix(src, options.inputPoints, options.subPixWinSize, New cv.Size(-1, -1), term)
            End If
            outputMat = cv.Mat.FromPixelData(options.inputPoints.Length, 1, cv.MatType.CV_32FC2, options.inputPoints)
            status = cv.Mat.FromPixelData(outputMat.Rows, outputMat.Cols, cv.MatType.CV_8U, 1)
        ElseIf options.inputPoints.Length > 0 Then
            Dim err As New cv.Mat
            ' convert the point2f vector to an inputarray (cv.Mat)
            Dim inputMat = cv.Mat.FromPixelData(options.inputPoints.Length, 1, cv.MatType.CV_32FC2, options.inputPoints)
            outputMat = inputMat.Clone()
            cv.Cv2.CalcOpticalFlowPyrLK(lastGray, src, inputMat, outputMat, status, err, options.winSize, 3, term, cv.OpticalFlowFlags.None)

            Dim k As Integer
            For i = 0 To options.inputPoints.Length - 1
                If status.Get(Of Byte)(i) Then
                    options.inputPoints(k) = outputMat.Get(Of cv.Point2f)(i)
                    k += 1
                End If
            Next
            ReDim Preserve options.inputPoints(k - 1)
        End If

        For i = 0 To outputMat.Rows - 1
            Dim pt = outputMat.Get(Of cv.Point2f)(i)
            If pt.X >= 0 And pt.X <= src.Cols And pt.Y >= 0 And pt.Y <= src.Rows Then
                If status.Get(Of Byte)(i) Then
                    DrawCircle(dst2,pt, task.DotSize + 1, circleColor)
                End If
            Else
                status.Set(Of Byte)(i, 0) ' this point is not visible!
            End If
        Next

        lastGray = src.Clone()
        labels(2) = "KLT Basics - " + If(options.inputPoints Is Nothing, "0", CStr(options.inputPoints.Length)) + " points"
    End Sub
End Class



' https://github.com/opencv/opencv/blob/master/samples/python/lk_track.py
Public Class KLT_OpticalFlow : Inherits VB_Parent
    Dim klt As New KLT_Basics
    Dim lastpoints() As cv.Point2f
    Public Sub New()
        desc = "KLT optical flow - needs more work"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        klt.Run(src)
        If task.frameCount > 0 And lastpoints IsNot Nothing And klt.options.inputPoints IsNot Nothing Then
            dst2 = klt.dst2
            src.CopyTo(dst3)
            For i = 0 To klt.options.inputPoints.Length - 1
                If klt.status.Get(Of Byte)(i) And i < lastpoints.Length And i < klt.options.inputPoints.Length Then
                    ' DrawLine(dst2,lastpoints(i), klt.inputPoints(i), cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
                    'Static lastFlowPoints() As cv.Point2f = klt.inputPoints
                    ' DrawLine(dst3, lastFlowPoints(i), klt.inputPoints(i), cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
                    'If task.heartBeat Then lastFlowPoints = klt.inputPoints
                End If
            Next
        End If
        lastpoints = klt.options.inputPoints
    End Sub
End Class



