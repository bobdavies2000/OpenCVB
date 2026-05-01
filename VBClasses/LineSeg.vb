Imports cv = OpenCvSharp
Public Class LineSeg_Basics : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Dim lsd As cv.LineSegmentDetector
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Cursor.ai: Use Line Segment Detector (LSD) to find lines in the image."
        lsd = cv.LineSegmentDetector.Create()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Or src.Type <> cv.MatType.CV_8U Then src = task.gray.Clone

        Dim vecMat As New cv.Mat
        lsd.Detect(src, vecMat)
        Dim vecArray() As cv.Vec4f = Nothing
        vecMat.GetArray(Of cv.Vec4f)(vecArray)
        lpList = Line_Basics_TA.getRawLines(vecArray)

        dst3.SetTo(0)
        dst2 = task.color.Clone
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            lp.index = i
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, cv.LineTypes.Link4)
            DrawLine(dst2, lp.p1, lp.p2, lp.color, task.lineWidth + 1)
        Next
        labels(2) = CStr(lpList.Count) + " LSD line segments were detected."
    End Sub
    Protected Overrides Sub Finalize()
        lsd.Dispose()
    End Sub
End Class






Public Class LineSeg_BasicsNew : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Private lSeg As New LineSeg_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Cursor.ai: toss existing lines where there is motion and add lines found in motion mask."
    End Sub
    Private Shared Function MatchScore(lp As lpData, candidate As lpData) As Single
        Dim centerScore = lp.ptCenter.DistanceTo(candidate.ptCenter)
        Dim sameEndpointOrder = lp.p1.DistanceTo(candidate.p1) + lp.p2.DistanceTo(candidate.p2)
        Dim swappedEndpointOrder = lp.p1.DistanceTo(candidate.p2) + lp.p2.DistanceTo(candidate.p1)
        Dim endpointScore = Math.Min(sameEndpointOrder, swappedEndpointOrder)
        Dim angleScore = Math.Abs(lp.angle - candidate.angle) * 5.0F
        Return centerScore + endpointScore + angleScore
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        lSeg.Run(src)
        Dim detected As New List(Of lpData)(lSeg.lpList)
        If detected.Count = 0 Then Exit Sub

        If lpList.Count = 0 Or task.optionsChanged Then
            lpList = New List(Of lpData)(detected)
        Else
            Dim used As New HashSet(Of Integer)
            For i = 0 To lpList.Count - 1
                Dim lp = lpList(i)
                If lp.rect.Width <= 0 Or lp.rect.Height <= 0 Then Continue For
                If task.motion.motionMask(lp.rect).CountNonZero = 0 Then Continue For

                Dim bestIndex As Integer = -1
                Dim bestScore As Single = Single.MaxValue
                For j = 0 To detected.Count - 1
                    If used.Contains(j) Then Continue For
                    Dim score = MatchScore(lp, detected(j))
                    If score < bestScore Then
                        bestScore = score
                        bestIndex = j
                    End If
                Next
                If bestIndex >= 0 Then
                    lpList(i) = detected(bestIndex)
                    used.Add(bestIndex)
                End If
            Next
        End If

        dst3.SetTo(0)
        dst2 = task.color.Clone
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            lp.index = i
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, cv.LineTypes.Link4)
            DrawLine(dst2, lp.p1, lp.p2, lp.color, task.lineWidth + 1)
        Next
        labels(2) = CStr(lpList.Count) + " lines in LineSeg_BasicsNew (updated only where motion exists)."
        labels(3) = CStr(detected.Count) + " lines detected this frame from LineSeg_Basics."
    End Sub
End Class