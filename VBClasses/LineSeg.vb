Imports cv = OpenCvSharp
Public Class LineSeg_Basics : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Public lpLast As New List(Of lpData)
    Dim core As New LineSeg_Core
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Keep prior lines with no motion; add lines on motion (CalcHist). Colors follow best match to previous frame."
    End Sub
    Private Shared Function MatchScoreForColor(a As lpData, b As lpData) As Single
        Dim centerScore = a.ptCenter.DistanceTo(b.ptCenter)
        Dim sameEndpointOrder = a.p1.DistanceTo(b.p1) + a.p2.DistanceTo(b.p2)
        Dim swappedEndpointOrder = a.p1.DistanceTo(b.p2) + a.p2.DistanceTo(b.p1)
        Dim endpointScore = Math.Min(sameEndpointOrder, swappedEndpointOrder)
        Dim angleScore = Math.Abs(a.angle - b.angle) * 5.0F
        Return centerScore + endpointScore + angleScore
    End Function
    Private Shared Function MotionTouchesLp(lp As lpData) As Boolean
        Dim mm = task.motion.motionMask
        If lp.rect.Width > 0 And lp.rect.Height > 0 Then
            Dim r = ValidateRect(lp.rect)
            If r.Width > 0 And r.Height > 0 AndAlso mm(r).CountNonZero > 0 Then Return True
        End If
        Dim pts = {lp.p1, lp.p2, lp.ptCenter}
        For Each pt In pts
            Dim x = CInt(pt.X)
            Dim y = CInt(pt.Y)
            If x < 0 Or x >= mm.Width Or y < 0 Or y >= mm.Height Then Continue For
            If mm.Get(Of Byte)(y, x) <> 0 Then Return True
        Next
        Return False
    End Function
    Public Shared Function lineHistogram(input As cv.Mat, nMax As Integer) As Single()
        Dim histogram As New cv.Mat
        cv.Cv2.CalcHist({input}, {0}, emptyMat, histogram, 1, {nMax},
                         New cv.Rangef() {New cv.Rangef(-1, nMax + 1)})

        Dim histArray(histogram.Total - 1) As Single
        histogram.GetArray(Of Single)(histArray)
        Return histArray
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then lpList.Clear()

        lpLast = New List(Of lpData)(lpList)
        core.Run(task.gray)
        Dim tmpList = core.lpList

        Dim outList As New List(Of lpData)
        For Each lp In lpLast
            If Not MotionTouchesLp(lp) Then outList.Add(lp)
        Next
        Dim retainedPrior = outList.Count

        If tmpList.Count > 0 Then
            Dim masked As New cv.Mat(core.dst1.Size, core.dst1.Type, 0)
            core.dst1.CopyTo(masked, task.motion.motionMask)
            masked.CopyTo(dst1)

            Dim histArray = lineHistogram(dst1, tmpList.Count)

            For i = 0 To tmpList.Count - 1
                If histArray(i) > 0 Then outList.Add(tmpList(i))
            Next
        Else
            dst1.SetTo(0)
        End If
        Dim motionAddCount = outList.Count - retainedPrior

        lpList = outList
        For i = 0 To lpList.Count - 1
            lpList(i).index = i
        Next

        Const colorMatchThreshold As Single = 150.0F
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            Dim bestScore As Single = Single.MaxValue
            Dim bestColor = lp.color
            For Each prev In lpLast
                Dim s = MatchScoreForColor(prev, lp)
                If s < bestScore Then
                    bestScore = s
                    bestColor = prev.color
                End If
            Next
            If bestScore < colorMatchThreshold Then lp.color = bestColor
        Next

        dst3.SetTo(0)
        dst2 = task.color.Clone
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, cv.LineTypes.Link4)
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
        Next

        labels(2) = CStr(retainedPrior) + " prior line(s) kept (no motion), " +
                    CStr(motionAddCount) + " from LSD overlapping motion (CalcHist)."
        labels(3) = CStr(tmpList.Count) + " LSD lines; dst1 = line indices where motion mask preserved pixels."
    End Sub
End Class





Public Class LineSeg_Core : Inherits TaskParent
    Implements IDisposable
    Public lpList As New List(Of lpData)
    Dim lsd As cv.LineSegmentDetector
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Cursor.ai: Use Line Segment Detector (LSD) to find lines in the image."
        lsd = cv.LineSegmentDetector.Create()
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        If src.Channels <> 1 Then src = task.gray.Clone

        Dim vecMat As New cv.Mat
        lsd.Detect(src, vecMat)
        Dim vecArray() As cv.Vec4f = Nothing
        vecMat.GetArray(Of cv.Vec4f)(vecArray)
        lpList = Line_Core.getRawSortedLines(vecArray)

        dst1.SetTo(0)
        dst3.SetTo(0)
        dst2 = task.color.Clone
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            lp.index = i
            dst1.Line(lp.p1, lp.p2, lp.index + 1, task.lineWidth, cv.LineTypes.Link4)
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
        Next
        dst3 = dst1.Threshold(0, 255, cv.ThresholdTypes.Binary)
        labels(2) = CStr(lpList.Count) + " LSD line segments were detected."
    End Sub
    Protected Overrides Sub Finalize()
        lsd.Dispose()
    End Sub
End Class






Public Class LineSeg_BasicsFail : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Private lSeg As New LineSeg_Basics
    Public Sub New()
        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_8U, 0)
        desc = "Cursor.ai: Retain lpList lines when motion touches them; update from LSD only in stable (no-motion) regions."
    End Sub
    Private Shared Function MatchScore(lp As lpData, candidate As lpData) As Single
        Dim centerScore = lp.ptCenter.DistanceTo(candidate.ptCenter)
        Dim sameEndpointOrder = lp.p1.DistanceTo(candidate.p1) + lp.p2.DistanceTo(candidate.p2)
        Dim swappedEndpointOrder = lp.p1.DistanceTo(candidate.p2) + lp.p2.DistanceTo(candidate.p1)
        Dim endpointScore = Math.Min(sameEndpointOrder, swappedEndpointOrder)
        Dim angleScore = Math.Abs(lp.angle - candidate.angle) * 5.0F
        Return centerScore + endpointScore + angleScore
    End Function
    ''' <summary>True if motion mask is nonzero inside the line rect or at either endpoint or center.</summary>
    Private Shared Function MotionTouchesLp(lp As lpData) As Boolean
        Dim mm = task.motion.motionMask
        If lp.rect.Width > 0 And lp.rect.Height > 0 Then
            Dim r = ValidateRect(lp.rect)
            If r.Width > 0 And r.Height > 0 And mm(r).CountNonZero > 0 Then Return True
        End If
        Dim pts = {lp.p1, lp.p2, lp.ptCenter}
        For Each pt In pts
            Dim x = CInt(pt.X)
            Dim y = CInt(pt.Y)
            If x < 0 Or x >= mm.Width Or y < 0 Or y >= mm.Height Then Continue For
            If mm.Get(Of Byte)(y, x) <> 0 Then Return True
        Next
        Return False
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        lSeg.Run(src)
        Dim detected As New List(Of lpData)(lSeg.lpList)

        If lpList.Count = 0 Or task.optionsChanged Then
            lpList = New List(Of lpData)(detected)
        Else
            Dim nextList As New List(Of lpData)
            Dim used As New HashSet(Of Integer)
            For i = 0 To lpList.Count - 1
                Dim lp = lpList(i)
                If MotionTouchesLp(lp) Then
                    nextList.Add(lp)
                    Continue For
                End If
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
                    nextList.Add(detected(bestIndex))
                    used.Add(bestIndex)
                Else
                    nextList.Add(lp)
                End If
            Next
            lpList = nextList
        End If

        dst3.SetTo(0)
        dst2 = task.color.Clone
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            lp.index = i
            dst3.Line(lp.p1, lp.p2, 255, task.lineWidth, cv.LineTypes.Link4)
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 1, task.lineType)
        Next
        labels(2) = CStr(lpList.Count) + " lines (retained when motion; updated from LSD when stable)."
        labels(3) = CStr(detected.Count) + " lines detected this frame from LineSeg_Basics."
    End Sub
End Class





''' <summary>
''' LBD-style line binary descriptors: 256 bits (32 bytes) per segment from Sobel magnitude samples
''' in a 5×7 band around each line. OpenCvSharp 4.x does not expose cv::line_descriptor::BinaryDescriptor;
''' this matches the same descriptor layout (one uchar row per line) used for Hamming distance.
''' </summary>
Public Class LineSeg_LBD : Inherits TaskParent

    Public Const DescriptorBytes As Integer = 32
    Public Const BandAlong As Integer = 5
    Public Const BandAcross As Integer = 7

    Public lpList As New List(Of lpData)
    ''' <summary>One row per line, 32 bytes CV_8U (256 bits).</summary>
    Public descriptors As New cv.Mat
    Dim lineSeg As New LineSeg_Basics
    Public Sub New()
        desc = "Cursor.ai: LBD-style 256-bit binary descriptors for each line from LineSeg_Basics (Sobel magnitude, 5×7 band sampling)"
    End Sub
    Private Shared Function ClampF(v As Single, lo As Single, hi As Single) As Single
        If v < lo Then Return lo
        If v > hi Then Return hi
        Return v
    End Function
    Private Shared Function SampleMag(m As cv.Mat, x As Single, y As Single) As Single
        Dim xi = CInt(Math.Floor(x + 0.5F))
        Dim yi = CInt(Math.Floor(y + 0.5F))
        If xi < 0 Or yi < 0 Or xi >= m.Width Or yi >= m.Height Then Return 0
        Return m.Get(Of Single)(yi, xi)
    End Function
    ''' <summary>Fill samples(0..34) with gradient magnitude on a 5×7 grid across the line support region.</summary>
    Private Shared Sub FillBandSamples(m As cv.Mat, lp As lpData, samples As Single())
        Dim p1 = New cv.Point2f(lp.p1.X, lp.p1.Y)
        Dim p2 = New cv.Point2f(lp.p2.X, lp.p2.Y)
        Dim vx = p2.X - p1.X
        Dim vy = p2.Y - p1.Y
        Dim len = CSng(Math.Sqrt(vx * vx + vy * vy))
        If len < 1.0F Then len = 1.0F
        vx /= len
        vy /= len
        Dim nx = -vy
        Dim ny = vx
        Dim halfW = ClampF(len * 0.08F, 1.5F, 14.0F)

        Dim idx = 0
        For iy = 0 To BandAlong - 1
            Dim t = iy / (BandAlong - 1.0F)
            Dim bx = p1.X + (p2.X - p1.X) * t
            Dim by = p1.Y + (p2.Y - p1.Y) * t
            For ix = 0 To BandAcross - 1
                Dim off = (ix - (BandAcross \ 2)) * halfW
                Dim sx = bx + nx * off
                Dim sy = by + ny * off
                samples(idx) = SampleMag(m, sx, sy)
                idx += 1
            Next
        Next
    End Sub
    Private Shared Sub ComputeDescriptor256(samples As Single(), row As cv.Mat)
        Dim bits(255) As Boolean
        For b = 0 To 255
            Dim i1 = b Mod 35
            Dim i2 = (b * 17 + 13) Mod 35
            If i1 = i2 Then i2 = (i2 + 1) Mod 35
            bits(b) = samples(i1) > samples(i2)
        Next
        For byteIdx = 0 To DescriptorBytes - 1
            Dim v As Integer = 0
            For bit = 0 To 7
                If bits(byteIdx * 8 + bit) Then v = v Or (1 << bit)
            Next
            row.Set(Of Byte)(0, byteIdx, CByte(v And &HFF))
        Next
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lineSeg.Run(src)
        lpList = New List(Of lpData)(lineSeg.lpList)

        Dim gray As cv.Mat = task.gray
        If src.Channels = 1 AndAlso src.Type = cv.MatType.CV_8U Then gray = src

        If lpList.Count = 0 Then
            descriptors = New cv.Mat(0, DescriptorBytes, cv.MatType.CV_8U)
            dst2 = task.color.Clone
            labels(2) = "No lines detected."
            Exit Sub
        End If

        Using gx As New cv.Mat(), gy As New cv.Mat(), mag As New cv.Mat()
            cv.Cv2.Sobel(gray, gx, cv.MatType.CV_32F, 1, 0, 3)
            cv.Cv2.Sobel(gray, gy, cv.MatType.CV_32F, 0, 1, 3)
            cv.Cv2.Magnitude(gx, gy, mag)

            descriptors = New cv.Mat(lpList.Count, DescriptorBytes, cv.MatType.CV_8U)
            Dim samples(34) As Single
            For i = 0 To lpList.Count - 1
                Dim lp = lpList(i)
                lp.index = i
                FillBandSamples(mag, lp, samples)
                Using row = descriptors.Row(i)
                    ComputeDescriptor256(samples, row)
                End Using
            Next
        End Using

        dst2 = task.color.Clone
        For i = 0 To lpList.Count - 1
            Dim lp = lpList(i)
            Dim color = task.scalarColors((lp.index + 1) Mod 255)
            dst2.Line(lp.p1, lp.p2, color, task.lineWidth + 1, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " LineSeg_Basics lines, " + CStr(DescriptorBytes) + " bytes LBD-style descriptor per line."
        labels(3) = "Descriptors: CV_8U matrix " + CStr(descriptors.Rows) + " x " + CStr(descriptors.Cols) + " (Sobel magnitude band)."
    End Sub
End Class


''' <summary>
''' Match LineSeg_LBD binary line descriptors between consecutive frames 
''' (Hamming on 32-byte rows + center distance).
''' </summary>
Public Class LineSeg_Match : Inherits TaskParent
    Dim lbd As New LineSeg_LBD
    Private descPrev As New cv.Mat
    Private lpPrev As New List(Of lpData)
    ''' <summary>Current-frame lines that were matched to a previous-frame line.</summary>
    Public lpList As New List(Of lpData)
    ''' <summary>Previous-frame line paired with each entry in lpList (same order).</summary>
    Public lpPrevMatched As New List(Of lpData)
    ''' <summary>Hamming distance for each match (bit mismatches in 256-bit descriptor).</summary>
    Public hammingList As New List(Of Integer)
    Public Const MaxCenterDist As Single = 100.0F
    Public Const MaxHamming As Integer = 88
    Public Const GeoWeight As Single = 0.35F
    Public Sub New()
        desc = "Cursor.ai: Match LineSeg_LBD line descriptors frame-to-frame (Hamming + " +
               "center distance, greedy one-to-one)."
    End Sub
    Private Shared Function HammingRow(a As cv.Mat, rowA As Integer, b As cv.Mat, rowB As Integer) As Integer
        Dim sum = 0
        For c = 0 To LineSeg_LBD.DescriptorBytes - 1
            sum += BitCountByte(CByte(a.Get(Of Byte)(rowA, c) Xor b.Get(Of Byte)(rowB, c)))
        Next
        Return sum
    End Function
    Private Shared Function BitCountByte(v As Byte) As Integer
        Dim n = 0
        Dim x = CInt(v)
        While x <> 0
            n += 1
            x = x And (x - 1)
        End While
        Return n
    End Function
    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            descPrev = New cv.Mat(0, LineSeg_LBD.DescriptorBytes, cv.MatType.CV_8U)
            lpPrev.Clear()
        End If

        lbd.Run(src)
        Dim currDesc = lbd.descriptors
        Dim currLp = lbd.lpList

        lpList.Clear()
        lpPrevMatched.Clear()
        hammingList.Clear()

        dst2 = task.color.Clone
        dst3 = task.color.Clone

        If currDesc.Rows = 0 Then
            descPrev = New cv.Mat(0, LineSeg_LBD.DescriptorBytes, cv.MatType.CV_8U)
            lpPrev.Clear()
            labels(2) = "No lines from LineSeg_LBD."
            labels(3) = ""
            Exit Sub
        End If

        If descPrev.Rows = 0 Then
            currDesc.CopyTo(descPrev)
            lpPrev = New List(Of lpData)(currLp)
            labels(2) = "Stored first frame (" + CStr(currDesc.Rows) + " lines); matching starts next frame."
            labels(3) = ""
            Exit Sub
        End If

        Dim prevRows = descPrev.Rows
        Dim pairs As New List(Of Tuple(Of Single, Integer, Integer, Integer))()
        For i = 0 To currDesc.Rows - 1
            For j = 0 To prevRows - 1
                Dim dCenter = currLp(i).ptCenter.DistanceTo(lpPrev(j).ptCenter)
                If dCenter > MaxCenterDist Then Continue For
                Dim h = HammingRow(currDesc, i, descPrev, j)
                If h > MaxHamming Then Continue For
                Dim score = h + GeoWeight * dCenter
                pairs.Add(New Tuple(Of Single, Integer, Integer, Integer)(score, i, j, h))
            Next
        Next

        pairs = pairs.OrderBy(Function(t) t.Item1).ToList()
        Dim usedCurr As New HashSet(Of Integer)
        Dim usedPrev As New HashSet(Of Integer)
        For Each t In pairs
            Dim i = t.Item2
            Dim j = t.Item3
            Dim h = t.Item4
            If usedCurr.Contains(i) OrElse usedPrev.Contains(j) Then Continue For
            usedCurr.Add(i)
            usedPrev.Add(j)
            lpList.Add(currLp(i))
            lpPrevMatched.Add(lpPrev(j))
            hammingList.Add(h)
        Next

        For k = 0 To lpList.Count - 1
            Dim c = task.scalarColors((k + 1) Mod 255)
            dst2.Line(lpList(k).p1, lpList(k).p2, c, task.lineWidth + 2, task.lineType)
            dst3.Line(lpPrevMatched(k).p1, lpPrevMatched(k).p2, c, task.lineWidth + 2, task.lineType)
        Next

        currDesc.CopyTo(descPrev)
        lpPrev = New List(Of lpData)(currLp)

        labels(2) = CStr(lpList.Count) + " line(s) matched (Hamming + center gate)."
        labels(3) = CStr(currLp.Count) + " current lines, " + CStr(prevRows) + " prev lines compared."
    End Sub
End Class







''' <summary>
''' On each heartBeatLT, take the three longest LineSeg_LBD segments into fixed-color slots. Between LT beats,
''' match each active slot to any current LBD line (Hamming + center, same gates as LineSeg_Match); slots go
''' inactive when no valid match exists until the next heartBeatLT re-seeds from the top three longest.
''' </summary>
Public Class LineSeg_Top3 : Inherits TaskParent
    Private Const SlotCount As Integer = 3
    Private Const GeoWeightTop3 As Single = LineSeg_Match.GeoWeight

    Dim lbd As New LineSeg_LBD
    ''' <summary>Tracked lines in slot order (slot 0, 1, 2), subset when fewer than three lines exist.</summary>
    Public lpList As New List(Of lpData)

    Private slotActive(SlotCount - 1) As Boolean
    Private slotDesc As New cv.Mat(SlotCount, LineSeg_LBD.DescriptorBytes, cv.MatType.CV_8U)
    Private slotCenter(SlotCount - 1) As cv.Point2f
    ''' <summary>Current-frame index into lbd.lpList for this slot (-1 if none / lost).</summary>
    Private slotLineIdx(SlotCount - 1) As Integer

    Public Sub New()
        desc = "On heartBeatLT, pick the 3 longest LineSeg_LBD lines; track them across frames until lost or the next heartBeatLT."
    End Sub

    Private Shared Function BitCountTop3(v As Byte) As Integer
        Dim n = 0
        Dim x = CInt(v)
        While x <> 0
            n += 1
            x = x And (x - 1)
        End While
        Return n
    End Function

    Private Shared Function HammingDescRowsTop3(a As cv.Mat, rowA As Integer, b As cv.Mat, rowB As Integer) As Integer
        Dim sum = 0
        For c = 0 To LineSeg_LBD.DescriptorBytes - 1
            sum += BitCountTop3(CByte(a.Get(Of Byte)(rowA, c) Xor b.Get(Of Byte)(rowB, c)))
        Next
        Return sum
    End Function

    Private Shared Sub CopyDescRowTop3(src As cv.Mat, srcRow As Integer, dst As cv.Mat, dstRow As Integer)
        Using sRow = src.Row(srcRow), dRow = dst.Row(dstRow)
            sRow.CopyTo(dRow)
        End Using
    End Sub

    Public Overrides Sub RunAlg(src As cv.Mat)
        If task.optionsChanged Then
            For i = 0 To SlotCount - 1
                slotActive(i) = False
                slotLineIdx(i) = -1
            Next
            slotDesc.SetTo(0)
        End If

        lbd.Run(src)
        Dim currLp = lbd.lpList
        Dim currDesc = lbd.descriptors

        lpList.Clear()
        dst2 = task.color.Clone

        If currLp.Count = 0 OrElse currDesc.Rows = 0 Then
            For i = 0 To SlotCount - 1
                slotActive(i) = False
                slotLineIdx(i) = -1
            Next
            labels(2) = "No lines from LineSeg_LBD."
            labels(3) = ""
            Exit Sub
        End If

        Dim nLines = currLp.Count

        If task.heartBeatLT Then
            Dim mPick = Math.Min(SlotCount, nLines)
            Dim topIdx = Enumerable.Range(0, nLines).OrderByDescending(Function(i) currLp(i).length).Take(mPick).ToList()
            For i = 0 To SlotCount - 1
                slotActive(i) = False
                slotLineIdx(i) = -1
            Next
            For i = 0 To mPick - 1
                Dim li = topIdx(i)
                slotActive(i) = True
                CopyDescRowTop3(currDesc, li, slotDesc, i)
                slotCenter(i) = currLp(li).ptCenter
                slotLineIdx(i) = li
            Next
        Else
            Dim hadActive = False
            For i = 0 To SlotCount - 1
                If slotActive(i) Then hadActive = True : Exit For
            Next
            If hadActive Then
                For i = 0 To SlotCount - 1
                    If slotActive(i) Then slotLineIdx(i) = -1
                Next
                Dim pairs As New List(Of Tuple(Of Single, Integer, Integer))()
                For i = 0 To SlotCount - 1
                    If Not slotActive(i) Then Continue For
                    For j = 0 To nLines - 1
                        Dim d = currLp(j).ptCenter.DistanceTo(slotCenter(i))
                        If d > LineSeg_Match.MaxCenterDist Then Continue For
                        Dim h = HammingDescRowsTop3(currDesc, j, slotDesc, i)
                        If h > LineSeg_Match.MaxHamming Then Continue For
                        Dim score = h + GeoWeightTop3 * d
                        pairs.Add(New Tuple(Of Single, Integer, Integer)(score, i, j))
                    Next
                Next
                pairs = pairs.OrderBy(Function(p) p.Item1).ToList()
                Dim usedSlot As New HashSet(Of Integer)
                Dim usedLine As New HashSet(Of Integer)
                For Each p In pairs
                    Dim index = p.Item2
                    Dim lj = p.Item3
                    If Not slotActive(index) Then Continue For
                    If usedSlot.Contains(index) OrElse usedLine.Contains(lj) Then Continue For
                    usedSlot.Add(index)
                    usedLine.Add(lj)
                    slotLineIdx(index) = lj
                    CopyDescRowTop3(currDesc, lj, slotDesc, index)
                    slotCenter(index) = currLp(lj).ptCenter
                Next
                For i = 0 To SlotCount - 1
                    If Not slotActive(i) Then Continue For
                    If slotLineIdx(i) < 0 Then
                        slotActive(i) = False
                        slotLineIdx(i) = -1
                        Using row = slotDesc.Row(i)
                            row.SetTo(0)
                        End Using
                    End If
                Next
            End If
        End If

        For i = 0 To SlotCount - 1
            If Not slotActive(i) OrElse slotLineIdx(i) < 0 Then Continue For
            Dim li = slotLineIdx(i)
            If li >= nLines Then Continue For
            Dim lp = currLp(li)
            lp.color = task.scalarColors(i)
            lpList.Add(lp)
            dst2.Line(lp.p1, lp.p2, lp.color, task.lineWidth + 2, task.lineType)
        Next

        labels(2) = CStr(lpList.Count) + " LineSeg_LBD line(s) tracked (heartBeatLT re-picks top 3 by length)."
        labels(3) = CStr(nLines) + " LBD lines; heartBeatLT = " + CStr(task.heartBeatLT) + "."
    End Sub
End Class




Public Class LineSeg_FLD : Inherits TaskParent
    Dim lSeg As New LineSeg_Core
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Merge the results of Line Segment Descriptor and Fast Line Detector."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lSeg.Run(task.gray)
        dst2 = lSeg.dst3
        labels(2) = lSeg.labels(2)
        labels(1) = task.lines.labels(2)
        dst1 = task.lines.dst3

        dst3 = dst1 And dst2

        dst0.SetTo(0)
        lSeg.dst1.CopyTo(dst0, dst3)

        Dim histArray = LineSeg_Basics.lineHistogram(dst0, Math.Max(task.lines.lpList.Count, lSeg.lpList.Count))

        dst3.SetTo(0)
        For i = 0 To Math.Min(histArray.Count, lSeg.lpList.Count) - 1
            If histArray(i) > 5 Then
                dst3.Line(lSeg.lpList(i).p1, lSeg.lpList(i).p2, 255, task.lineWidth, task.lineType)
            End If
        Next
    End Sub
End Class




Public Class LineSeg_Detector : Inherits TaskParent
    Dim lSeg As New LineSeg_Core
    Public lpList As New List(Of lpData)
    Public Sub New()
        dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        If standalone Then task.gOptions.displayDst1.Checked = True
        desc = "Compare the results of the line segment detector and fast line detector."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        lSeg.Run(task.gray)
        dst2 = lSeg.dst3
        labels(2) = lSeg.labels(2)

        dst1 = task.lines.dst3
        labels(1) = task.lines.labels(2)

        dst3 = dst1 And dst2

        dst0.SetTo(0)
        lSeg.dst1.CopyTo(dst0, dst3)

        Dim histArray = LineSeg_Basics.lineHistogram(dst0, Math.Max(task.lines.lpList.Count, lSeg.lpList.Count))

        dst3.SetTo(0)
        For i = 0 To Math.Min(histArray.Count, lSeg.lpList.Count) - 1
            If histArray(i) > 5 Then
                dst3.Line(lSeg.lpList(i).p1, lSeg.lpList(i).p2, 255, task.lineWidth, task.lineType)
            End If
        Next
    End Sub
End Class





Public Class LineSeg_BasicsAlt : Inherits TaskParent
    Public lpList As New List(Of lpData)
    Dim lpFind As New Line_FindClosest
    Public core As New LineSeg_Core
    Public Sub New()
        desc = "Run FLD (Fast Line Detector) with sobel input."
    End Sub
    Public Overrides Sub RunAlg(src As cv.Mat)
        dst2 = task.color.Clone
        If src.Channels <> 1 Or src.Type <> cv.MatType.CV_8U Then src = task.gray.Clone

        core.Run(src)

        lpList.Clear()
        Dim removeNearDuplicates As Boolean = True
        If removeNearDuplicates Then
            Dim edgeMap As New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
            For Each lp In core.lpList
                Dim val1 = edgeMap.Get(Of Byte)(lp.ptE1.Y, lp.ptE1.X)
                Dim val2 = edgeMap.Get(Of Byte)(lp.ptE1.Y, lp.ptE1.X)
                If val1 > 0 And val2 > 0 Then Continue For

                lp.index = lpList.Count + 1

                Dim gridIndex = task.gridMap.Get(Of Integer)(Math.Floor(lp.ptE1.Y), Math.Floor(lp.ptE1.X))
                edgeMap(task.gridNabeRects(gridIndex)).SetTo(lp.index)
                lpList.Add(lp)

                Dim tierIndex = task.depthTiers.dst2.Get(Of Byte)(lp.p1.Y, lp.p1.X)
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 1, cv.LineTypes.Link4)
            Next
        Else
            For Each lp In core.lpList
                lp.index = lpList.Count + 1
                lpList.Add(lp)
                dst2.Line(lp.p1, lp.p2, task.highlight, task.lineWidth + 1, cv.LineTypes.Link4)
            Next
        End If

        Dim count As Integer
        For Each lp In task.lines.lpLast
            lpFind.inputLine = lp
            lpFind.Run(src)
            Dim closest = lpFind.closestLine
            If closest IsNot Nothing Then
                If closest.index < lpList.Count Then
                    Dim lpCurr = lpList(closest.index - 1)
                    lpCurr.age = lp.age + 1
                    lpCurr.indexLast = lp.index
                    If lpCurr.age >= 1000 Then lpCurr.age = 10
                    count += 1
                End If
            End If
            SetTrueText(CStr(lp.age), New cv.Point2f(lp.ptCenter.X + 2, lp.ptCenter.Y + 2), 2)
        Next

        Dim lpAgeSort As New SortedList(Of Integer, Integer)(New compareAllowIdenticalIntegerInverted)
        For Each lp In lpList
            lpAgeSort.Add(lp.age, lp.index)
        Next

        Static gravity = task.lpGravity
        If (task.longestLine = gravity Or task.longestLine Is Nothing) And lpList.Count > 0 Then task.longestLine = lpList(0)
        If lpList.Count > 0 Then
            lpFind.inputLine = If(task.longestLine Is Nothing, lpList(0), task.longestLine)
            lpFind.lpList = lpList
            lpFind.Run(emptyMat)
            Dim lpTmp = lpFind.closestLine

            If lpTmp Is Nothing Then
                gravity = task.lpGravity
                task.longestLine = task.lpGravity
            Else
                task.longestLine = New lpData(lpTmp.ptE1, lpTmp.ptE2)
                task.longestLine.age = lpTmp.age
            End If
        Else
            gravity = task.lpGravity
            task.longestLine = task.lpGravity
            lpList.Add(task.longestLine) ' need to always have something in lplist...
        End If

        Static minCount As Integer = count
        If task.heartBeat Then minCount = count
        If count < minCount Then minCount = count
        Dim ageCount = lpAgeSort.Keys.Count
        labels(2) = CStr(lpList.Count) + " lines found.  Value next to the line is the age.  Minimal count = " + CStr(minCount) +
                    " Average age = " + If(ageCount > 0, Format(lpAgeSort.Keys.Average, fmt1), "0")
    End Sub
End Class