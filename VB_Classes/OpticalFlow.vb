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
Public Class OpticalFlow_LucasKanade : Inherits VB_Algorithm
    Public features As New List(Of cv.Point2f)
    Public lastFeatures As New List(Of cv.Point2f)
    Dim feat As New Feature_Basics
    Dim options As New Options_OpticalFlowSparse
    Public Sub New()
        desc = "Show the optical flow of a sparse matrix."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        dst2 = src.Clone()
        dst3 = src.Clone()

        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        Static lastGray As cv.Mat = src.Clone
        feat.Run(src)
        features = task.features
        Dim features1 = New cv.Mat(features.Count, 1, cv.MatType.CV_32FC2, features.ToArray)
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
                    dst2.Line(pt1, pt2, task.highlightColor, task.lineWidth + task.lineWidth, task.lineType)
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






Public Class OpticalFlow_LeftRight1 : Inherits VB_Algorithm
    Dim pyrLeft As New OpticalFlow_LucasKanade
    Dim pyrRight As New OpticalFlow_LucasKanade
    Dim ptLeft As New List(Of cv.Point)
    Dim ptRight As New List(Of cv.Point)
    Public ptlist As New List(Of cv.Point)
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Find features using optical flow in both the left and right images."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static corrSlider = findSlider("Feature Correlation Threshold")
        Static cellSlider = findSlider("MatchTemplate Cell Size")
        Dim pad = CInt(cellSlider.value / 2)
        Dim gSize = cellSlider.value
        Dim correlationMin = corrSlider.value / 100

        pyrLeft.Run(task.leftView)
        pyrRight.Run(task.rightView)

        Dim leftY As New List(Of Integer)
        ptLeft.Clear()
        dst2 = task.leftView
        For i = 0 To pyrLeft.features.Count - 1
            Dim pt = pyrLeft.features(i)
            ptLeft.Add(New cv.Point(pt.X, pt.Y))
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            leftY.Add(pt.Y)

            pt = pyrLeft.lastFeatures(i)
            ptLeft.Add(New cv.Point(pt.X, pt.Y))
            dst2.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            leftY.Add(pt.Y)
        Next

        Dim rightY As New List(Of Integer)
        ptRight.Clear()
        dst3 = task.rightView
        For i = 0 To pyrRight.features.Count - 1
            Dim pt = pyrRight.features(i)
            ptRight.Add(New cv.Point(pt.X, pt.Y))
            dst3.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            rightY.Add(pt.Y)

            pt = pyrRight.lastFeatures(i)
            ptRight.Add(New cv.Point(pt.X, pt.Y))
            dst3.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
            rightY.Add(pt.Y)
        Next

        Dim mpList As New List(Of pointPair)
        ptlist.Clear()
        For i = 0 To leftY.Count - 1
            Dim index = rightY.IndexOf(leftY(i))
            If index >= 0 Then mpList.Add(New pointPair(ptLeft(i), ptRight(index)))
        Next

        'Dim correlationmat As New cv.Mat
        'Dim mpCorrelation As New List(Of Single)
        'For i = 0 To mpList.Count - 1
        '    Dim rect = validateRect(New cv.Rect(pt.X - pad, pt.Y - pad, gSize, gSize))
        '    Dim correlations As New List(Of Single)
        '    For Each ptRight In lrFeat.rightFeatures(i)
        '        Dim r = validateRect(New cv.Rect(ptRight.X - pad, ptRight.Y - pad, gSize, gSize))
        '        cv.Cv2.MatchTemplate(task.leftView(rect), task.rightView(r), correlationmat, cv.TemplateMatchModes.CCoeffNormed)
        '        correlations.Add(correlationmat.Get(Of Single)(0, 0))
        '    Next
        '    Dim maxCorrelation = correlations.Max
        '    If maxCorrelation >= correlationMin Then
        '        Dim index = correlations.IndexOf(maxCorrelation)
        '        mpList.Add(New pointPair(pt, lrFeat.rightFeatures(i)(index)))
        '        mpCorrelation.Add(maxCorrelation)
        '    End If
        'Next

        If task.heartBeat Then
            labels(2) = CStr(ptLeft.Count) + " features found in the left image, " + CStr(ptRight.Count) + " features in the right and " +
                        CStr(ptlist.Count) + " features are matched."
        End If
    End Sub
End Class






Public Class OpticalFlow_LeftRightHist : Inherits VB_Algorithm
    Dim pyrLeft As New OpticalFlow_LucasKanade
    Dim pyrRight As New OpticalFlow_LucasKanade
    Public leftPoints As New List(Of cv.Point)
    Public rightPoints As New List(Of cv.Point)
    Public Sub New()
        desc = "Keep only the features that have been around for the specified number of frames."
    End Sub
    Public Function displayFeatures(dst As cv.Mat, features As List(Of cv.Point)) As cv.Mat
        For Each pt In features
            dst.Circle(pt, task.dotSize, task.highlightColor, -1, task.lineType)
        Next
        Return dst
    End Function
    Public Sub RunVB(src As cv.Mat)
        pyrLeft.Run(task.leftView)
        Dim tmpLeft As New List(Of cv.Point)
        For i = 0 To pyrLeft.features.Count - 1
            Dim pt = New cv.Point(pyrLeft.features(i).X, pyrLeft.features(i).Y)
            tmpLeft.Add(New cv.Point(pt.X, pt.Y))
            pt = New cv.Point(pyrLeft.lastFeatures(i).X, pyrLeft.lastFeatures(i).Y)
            tmpLeft.Add(New cv.Point(pt.X, pt.Y))
        Next

        pyrRight.Run(task.rightView)
        Dim tmpRight As New List(Of cv.Point)
        For i = 0 To pyrRight.features.Count - 1
            Dim pt = New cv.Point(pyrRight.features(i).X, pyrRight.features(i).Y)
            tmpRight.Add(New cv.Point(pt.X, pt.Y))
            pt = New cv.Point(pyrRight.lastFeatures(i).X, pyrRight.lastFeatures(i).Y)
            tmpRight.Add(New cv.Point(pt.X, pt.Y))
        Next

        Static leftHist As New List(Of List(Of cv.Point))({tmpLeft})
        Static rightHist As New List(Of List(Of cv.Point))({tmpRight})

        If task.optionsChanged Then
            leftHist = New List(Of List(Of cv.Point))({tmpLeft})
            rightHist = New List(Of List(Of cv.Point))({tmpRight})
        End If

        leftPoints.Clear()
        For Each pt In tmpLeft
            Dim count As Integer = 0
            For Each hist In leftHist
                If hist.Contains(pt) Then count += 1 Else Exit For
            Next
            If count = leftHist.Count Then leftPoints.Add(pt)
        Next

        rightPoints.Clear()
        For Each pt In tmpRight
            Dim count As Integer = 0
            For Each hist In rightHist
                If hist.Contains(pt) Then count += 1 Else Exit For
            Next
            If count = rightHist.Count Then rightPoints.Add(pt)
        Next

        Dim minPoints = 10 ' just a guess - trying to keep things current.
        If leftPoints.Count < minPoints Then
            leftPoints = tmpLeft
            leftHist = New List(Of List(Of cv.Point))({tmpLeft})
        End If
        If rightPoints.Count < minPoints Then
            rightPoints = tmpRight
            rightHist = New List(Of List(Of cv.Point))({tmpRight})
        End If

        dst2 = displayFeatures(task.leftView, leftPoints)
        dst3 = displayFeatures(task.rightView, rightPoints)

        leftHist.Add(tmpLeft)
        rightHist.Add(tmpRight)
        Dim threshold = Math.Min(gOptions.FrameHistory.Value, leftHist.Count)

        If leftHist.Count >= gOptions.FrameHistory.Value Then leftHist.RemoveAt(0)
        If rightHist.Count >= gOptions.FrameHistory.Value Then rightHist.RemoveAt(0)

        If task.heartBeat Then
            labels(2) = CStr(leftPoints.Count) + " detected in the left image that have matches in " + CStr(threshold) + " previous left images"
            labels(3) = CStr(rightPoints.Count) + " detected in the right image that have matches in " + CStr(threshold) + " previous right images"
        End If
    End Sub
End Class