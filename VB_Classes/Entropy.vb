Imports cv = OpenCvSharp
' http://areshopencvb.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics : Inherits TaskParent
    Dim entropy As New Entropy_Rectangle
    Public Sub New()
        labels(2) = "Control entropy values with histogram bins slider"
        desc = "Compute the entropy in an image - a measure of contrast(iness)"
    End Sub
    Private Function validatePreserve(ByVal r As cv.Rect) As cv.Rect
        If r.Width <= 0 Then r.Width = 1
        If r.Height <= 0 Then r.Height = 1
        If r.X < 0 Then r.X = 0
        If r.Y < 0 Then r.Y = 0
        If r.X + r.Width >= task.dst2.Width Then r.X = task.dst2.Width - r.Width - 1
        If r.Y + r.Height >= task.dst2.Height Then r.Y = task.dst2.Height - r.Height - 1
        Return r
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim stdSize = 30
        If task.drawRect = New cv.Rect Then
            task.drawRect = New cv.Rect(30, 30, stdSize, stdSize) ' arbitrary rectangle
        End If
        If task.mouseClickFlag Then
            task.drawRect = validatePreserve(New cv.Rect(task.ClickPoint.X, task.ClickPoint.Y, stdSize, stdSize))
        End If
        task.drawRect = ValidateRect(task.drawRect)
        If src.Channels() = 3 Then
            entropy.Run(src.CvtColor(cv.ColorConversionCodes.BGR2Gray)(task.drawRect))
        Else
            entropy.Run(src(task.drawRect))
        End If
        dst2 = entropy.dst2
        dst2.Rectangle(task.drawRect, white, task.lineWidth)
        If task.heartBeat Then strOut = "Click anywhere to measure the entropy with rect(pt.x, pt.y, " +
                                         CStr(stdSize) + ", " + CStr(stdSize) + ")" + vbCrLf + vbCrLf + "Total entropy = " +
                                         Format(entropy.entropyVal, fmt1) + vbCrLf + entropy.strOut
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Entropy_Highest : Inherits TaskParent
    Dim entropy As New Entropy_Rectangle
    Public eMaxRect As cv.Rect
    Public Sub New()
        If standalone Then task.gOptions.GridSlider.Value = CInt(dst2.Width / 10)
        desc = "Find the highest entropy section of the color image."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim entropyMap = New cv.Mat(src.Size(), cv.MatType.CV_32F)
        Dim entropyList(task.gridRects.Count - 1) As Single
        Dim maxEntropy As Single = Single.MinValue
        Dim minEntropy As Single = Single.MaxValue
        trueData.Clear()

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        For Each roi In task.gridRects
            If roi.Width = roi.Height Then
                entropy.Run(src(roi))
                entropyMap(roi).SetTo(entropy.entropyVal)

                If entropy.entropyVal > maxEntropy Or task.optionsChanged Then
                    maxEntropy = entropy.entropyVal
                    eMaxRect = roi
                End If
                If entropy.entropyVal < minEntropy Then minEntropy = entropy.entropyVal
                If standaloneTest() And task.cellSize > 16 Then
                    Dim pt = New cv.Point(roi.X, roi.Y)
                    SetTrueText(Format(entropy.entropyVal, fmt2), pt, 2)
                    SetTrueText(Format(entropy.entropyVal, fmt2), pt, 3)
                End If
            End If
        Next

        dst2 = entropyMap.ConvertScaleAbs(255 / (maxEntropy - minEntropy), minEntropy)
        dst2 = ShowAddweighted(src, dst2, labels(3))

        If standaloneTest() Then
            dst2.Rectangle(eMaxRect, 255, task.lineWidth)
            dst3.SetTo(0)
            dst3.Rectangle(eMaxRect, white, task.lineWidth)
        End If
        labels(2) = "Lighter = higher entropy. Range: " + Format(minEntropy, "0.0") + " to " + Format(maxEntropy, "0.0")
    End Sub
End Class






Public Class Entropy_FAST : Inherits TaskParent
    Dim fast As New Corners_Basics
    Dim entropy As New Entropy_Highest
    Public Sub New()
        labels = {"", "", "Output of Corners_FAST, input to entropy calculation", "Lighter color is higher entropy, highlight shows highest"}
        desc = "Use FAST markings to add to entropy"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        fast.Run(src)

        entropy.Run(fast.dst2)
        dst2 = entropy.dst2
        dst3 = entropy.dst2
        dst3.Rectangle(entropy.eMaxRect, task.HighlightColor, task.lineWidth)
    End Sub
End Class





Public Class Entropy_Rectangle : Inherits TaskParent
    Public entropyVal As Single
    Public Sub New()
        desc = "Calculate the entropy in the drawRect when run standalone"
    End Sub
    Public Function channelEntropy(total As Integer, hist As cv.Mat) As Single
        channelEntropy = 0
        For i = 0 To hist.Rows - 1
            Dim hc = Math.Abs(hist.Get(Of Single)(i))
            If hc <> 0 Then channelEntropy += -(hc / total) * Math.Log10(hc / total)
        Next
        Return channelEntropy
    End Function
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim dimensions() = New Integer() {task.histogramBins}
        If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2Gray)

        Dim mm = GetMinMax(src)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
        If mm.minVal = mm.maxVal Then ranges = New cv.Rangef() {New cv.Rangef(0, 255)}

        If standalone Then
            If task.drawRect.Width = 0 Or task.drawRect.Height = 0 Then
                task.drawRect = New cv.Rect(10, 10, 50, 50) ' arbitrary template to match
            End If
            src = src(task.drawRect)
        End If
        Dim hist As New cv.Mat
        cv.Cv2.CalcHist({src}, {0}, New cv.Mat(), hist, 1, dimensions, ranges)
        Dim histNormalized = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

        entropyVal = channelEntropy(src.Total, histNormalized) * 1000
        strOut = "Entropy X1000 " + Format(entropyVal, fmt1) + vbCrLf
        dst2 = src
        dst2.Rectangle(task.drawRect, white, task.lineWidth)
        dst3 = src
        SetTrueText(strOut, 3)
    End Sub
End Class






Public Class Entropy_SubDivisions : Inherits TaskParent
    Dim entropy As New Entropy_Rectangle
    Dim entropies As New List(Of List(Of Single))
    Dim eROI As New List(Of List(Of cv.Rect))
    Public roiList As New List(Of cv.Rect)
    Public Sub New()
        labels(2) = "The top entropy values in each subdivision"
        For i = 0 To task.subDivisionCount - 1
            entropies.Add(New List(Of Single)) ' 4 quadrants
            eROI.Add(New List(Of cv.Rect)) ' 4 quadrants
        Next
        desc = "Find the highest entropy in each quadrant"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        dst2 = task.color.Clone
        For i = 0 To task.subDivisionCount - 1
            entropies(i).Clear()
            eROI(i).Clear()
        Next

        dst1 = If(src.Channels() = 1, src, src.CvtColor(cv.ColorConversionCodes.BGR2Gray))
        Dim dimensions() = New Integer() {task.histogramBins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 255)}
        Dim hist As New cv.Mat
        For i = 0 To task.gridRects.Count - 1
            Dim roi = task.gridRects(i)
            cv.Cv2.CalcHist({dst1(roi)}, {0}, New cv.Mat(), hist, 1, dimensions, ranges)
            hist = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

            Dim nextEntropy = entropy.channelEntropy(dst1(roi).Total, hist) * 1000

            entropies(task.subDivisions(i)).Add(nextEntropy)
            eROI(task.subDivisions(i)).Add(roi)
            If standaloneTest() Then SetTrueText(Format(nextEntropy, fmt2), New cv.Point(roi.X, roi.Y), 3)
        Next

        roiList.Clear()
        For i = 0 To task.subDivisionCount - 1
            Dim eList = entropies(i)
            Dim maxEntropy = eList.Max
            Dim roi = eROI(i)(eList.IndexOf(maxEntropy))
            roiList.Add(roi)
            dst2.Rectangle(roi, white)
        Next

        Dim p1 = New cv.Point(0, dst2.Height / 3)
        Dim p2 = New cv.Point(dst2.Width, dst2.Height / 3)
        DrawLine(dst2, p1, p2, white)
        p1 = New cv.Point(0, dst2.Height * 2 / 3)
        p2 = New cv.Point(dst2.Width, dst2.Height * 2 / 3)
        DrawLine(dst2, p1, p2, white)
        p1 = New cv.Point(dst2.Width / 3, 0)
        p2 = New cv.Point(dst2.Width / 3, dst2.Height)
        DrawLine(dst2, p1, p2, white)
        p1 = New cv.Point(dst2.Width * 2 / 3, 0)
        p2 = New cv.Point(dst2.Width * 2 / 3, dst2.Height)
        DrawLine(dst2, p1, p2, white)
    End Sub
End Class






Public Class Entropy_BinaryImage : Inherits TaskParent
    Dim binary As New Binarize_Simple
    Dim entropy As New Entropy_Basics
    Public Sub New()
        desc = "Measure entropy in a binary image"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        binary.Run(src)
        dst2 = binary.dst2
        labels(2) = binary.labels(2)

        entropy.Run(dst2)
        SetTrueText(entropy.strOut, 3)
    End Sub
End Class
