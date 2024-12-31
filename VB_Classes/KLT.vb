Imports cvb = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/lkdemo.cpp
Public Class KLT_Basics : Inherits TaskParent
    Public status As New cvb.Mat
    Public outputMat As New cvb.Mat
    Public circleColor = cvb.Scalar.Red
    Public options As New Options_KLT
    Public ptInput() As cvb.Point2f
    Public Sub New()
        term = New cvb.TermCriteria(cvb.CriteriaTypes.Eps Or cvb.CriteriaTypes.Count, 10, 1.0)
        desc = "Track movement with Kanada-Lucas-Tomasi algorithm"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        Options.RunOpt()

        If options.nightMode Then dst2.SetTo(0) Else src.CopyTo(dst2)
        Static lastGray As cvb.Mat = src.Clone

        If src.Channels() = 3 Then src = src.CvtColor(cvb.ColorConversionCodes.BGR2Gray)
        If options.ptInput Is Nothing Then
            options.ptInput = cvb.Cv2.GoodFeaturesToTrack(src, options.maxCorners, options.qualityLevel,
                                                             options.minDistance, New cvb.Mat, options.blockSize, False, 0)
            If options.ptInput.Length > 0 Then
                options.ptInput = cvb.Cv2.CornerSubPix(src, options.ptInput, options.subPixWinSize, New cvb.Size(-1, -1), term)
            End If
            outputMat = cvb.Mat.FromPixelData(options.ptInput.Length, 1, cvb.MatType.CV_32FC2, options.ptInput)
            status = New cvb.Mat(outputMat.Rows, outputMat.Cols, cvb.MatType.CV_8U, cvb.Scalar.All(1))
        ElseIf options.ptInput.Length > 0 Then
            Dim err As New cvb.Mat
            ' convert the point2f vector to an inputarray (cvb.Mat)
            Dim inputMat = cvb.Mat.FromPixelData(options.ptInput.Length, 1, cvb.MatType.CV_32FC2, options.ptInput)
            outputMat = inputMat.Clone()
            cvb.Cv2.CalcOpticalFlowPyrLK(lastGray, src, inputMat, outputMat, status, err, options.winSize, 3, term, cvb.OpticalFlowFlags.None)

            Dim k As Integer
            For i = 0 To options.ptInput.Length - 1
                If status.Get(Of Byte)(i) Then
                    options.ptInput(k) = outputMat.Get(Of cvb.Point2f)(i)
                    k += 1
                End If
            Next
            ReDim Preserve options.ptInput(k - 1)
        End If

        For i = 0 To outputMat.Rows - 1
            Dim pt = outputMat.Get(Of cvb.Point2f)(i)
            If pt.X >= 0 And pt.X <= src.Cols And pt.Y >= 0 And pt.Y <= src.Rows Then
                If status.Get(Of Byte)(i) Then
                    DrawCircle(dst2,pt, task.DotSize + 1, circleColor)
                End If
            Else
                status.Set(Of Byte)(i, 0) ' this point is not visible!
            End If
        Next

        lastGray = src.Clone()
        labels(2) = "KLT Basics - " + If(options.ptInput Is Nothing, "0", CStr(options.ptInput.Length)) + " points"
    End Sub
End Class



' https://github.com/opencv/opencv/blob/master/samples/python/lk_track.py
Public Class KLT_OpticalFlow : Inherits TaskParent
    Dim klt As New KLT_Basics
    Dim lastpoints() As cvb.Point2f
    Public Sub New()
        desc = "KLT optical flow - needs more work"
    End Sub
    Public Overrides sub runAlg(src As cvb.Mat)
        klt.Run(src)
        If task.frameCount > 0 And lastpoints IsNot Nothing And klt.ptInput IsNot Nothing Then
            dst2 = klt.dst2
            src.CopyTo(dst3)
            For i = 0 To klt.ptInput.Length - 1
                If klt.status.Get(Of Byte)(i) And i < lastpoints.Length And i < klt.ptInput.Length Then
                    ' DrawLine(dst2,lastpoints(i), klt.inputPoints(i), cvb.Scalar.Yellow, task.lineWidth + 1, task.lineType)
                    'Static lastFlowPoints() As cvb.Point2f = klt.inputPoints
                    ' DrawLine(dst3, lastFlowPoints(i), klt.inputPoints(i), cvb.Scalar.Yellow, task.lineWidth + 1, task.lineType)
                    'If task.heartBeat Then lastFlowPoints = klt.inputPoints
                End If
            Next
        End If
        lastpoints = klt.ptInput
    End Sub
End Class



