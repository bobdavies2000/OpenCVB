Imports cv = OpenCvSharp
' https://github.com/opencv/opencv/blob/master/samples/cpp/lkdemo.cpp
Public Class KLT_Basics
    Inherits VBparent
    Public inputPoints() As cv.Point2f
    Public status As New cv.Mat
    Public outputMat As New cv.Mat
    Public circleColor = cv.Scalar.Red
    Dim term As New cv.TermCriteria(cv.CriteriaTypes.Eps + cv.CriteriaTypes.Count, 10, 1.0)
    Public Sub New()
        initParent()
        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "KLT - MaxCorners", 1, 200, 100)
            sliders.setupTrackBar(1, "KLT - qualityLevel", 1, 100, 1) ' low quality!  We want lots of points.
            sliders.setupTrackBar(2, "KLT - minDistance", 1, 100, 7)
            sliders.setupTrackBar(3, "KLT - BlockSize", 1, 100, 7)
        End If

        If findfrm(caller + " CheckBox Options") Is Nothing Then
            check.Setup(caller, 2)
            check.Box(0).Text = "KLT - Night Mode"
            check.Box(1).Text = "KLT - delete all Points"
        End If
        task.desc = "Track movement with Kanada-Lucas-Tomasi algorithm"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        Static prevGray As New cv.Mat

        If check.Box(1).Checked Or task.frameCount Mod 25 = 0 Then
            inputPoints = Nothing ' just delete all points and start again.
            check.Box(1).Checked = False
        End If

        Dim maxCorners = sliders.trackbar(0).Value
        Dim qualityLevel = sliders.trackbar(1).Value / 100
        Dim minDistance = sliders.trackbar(2).Value
        Dim blockSize = sliders.trackbar(3).Value
        Dim winSize As New cv.Size(3, 3)
        Dim subPixWinSize As New cv.Size(10, 10)
        Dim nightMode = check.Box(0).Checked

        If nightMode Then dst1.SetTo(0) Else src.CopyTo(dst1)

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        If inputPoints Is Nothing Then
            inputPoints = cv.Cv2.GoodFeaturesToTrack(src, maxCorners, qualityLevel, minDistance, New cv.Mat, blockSize, False, 0)
            If inputPoints.Length > 0 Then
                inputPoints = cv.Cv2.CornerSubPix(src, inputPoints, subPixWinSize, New cv.Size(-1, -1), term)
            End If
            outputMat = New cv.Mat(inputPoints.Length, 1, cv.MatType.CV_32FC2, inputPoints)
            status = New cv.Mat(outputMat.Rows, outputMat.Cols, cv.MatType.CV_8U, 1)
        ElseIf inputPoints.Length > 0 Then
            Dim err As New cv.Mat
            ' convert the point2f vector to an inputarray (cv.Mat)
            Dim inputMat = New cv.Mat(inputPoints.Length, 1, cv.MatType.CV_32FC2, inputPoints)
            outputMat = inputMat.Clone()
            cv.Cv2.CalcOpticalFlowPyrLK(prevGray, src, inputMat, outputMat, status, err, winSize, 3, term, cv.OpticalFlowFlags.None)

            Dim k As integer
            For i = 0 To inputPoints.Length - 1
                If status.Get(Of Byte)(i) Then
                    inputPoints(k) = outputMat.Get(Of cv.Point2f)(i)
                    k += 1
                End If
            Next
            ReDim Preserve inputPoints(k - 1)
        End If

        For i = 0 To outputMat.Rows - 1
            Dim pt = outputMat.Get(Of cv.Point2f)(i)
            If pt.X >= 0 And pt.X <= src.Cols And pt.Y >= 0 And pt.Y <= src.Rows Then
                If status.Get(Of Byte)(i) Then
                    dst1.Circle(pt, 3, circleColor, -1, task.lineType)
                End If
            Else
                status.Set(Of Byte)(i, 0) ' this point is not visible!
            End If
        Next

        prevGray = src.Clone()
        label1 = "KLT Basics - " + If(inputPoints Is Nothing, "0", CStr(inputPoints.Length)) + " points"
    End Sub
End Class



' https://github.com/opencv/opencv/blob/master/samples/python/lk_track.py
Public Class KLT_OpticalFlow
    Inherits VBparent
    Dim klt As KLT_Basics
    Dim lastpoints() As cv.Point2f
    Public Sub New()
        initParent()
        klt = New KLT_Basics()
        task.desc = "KLT optical flow - needs more work"
		' task.rank = 1
    End Sub
    Public Sub Run(src as cv.Mat)
        klt.Run(src)
        If task.frameCount > 0 And lastpoints IsNot Nothing And klt.inputPoints IsNot Nothing Then
            dst1 = klt.dst1
            src.CopyTo(dst2)
            For i = 0 To klt.inputPoints.Length - 1
                If klt.status.Get(Of Byte)(i) And i < lastpoints.Length And i < klt.inputPoints.Length Then
                    ' dst1.Line(lastpoints(i), klt.inputPoints(i), cv.Scalar.Yellow, 2, task.lineType)
                    'Static lastFlowPoints() As cv.Point2f = klt.inputPoints
                    ' dst2.Line(lastFlowPoints(i), klt.inputPoints(i), cv.Scalar.Yellow, 2, task.lineType)
                    'If task.frameCount Mod 10 = 0 Then lastFlowPoints = klt.inputPoints
                End If
            Next
        End If
        lastpoints = klt.inputPoints
    End Sub
End Class



