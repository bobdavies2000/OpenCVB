Imports cv = OpenCvSharp
' http://areshopencv.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics : Inherits VB_Algorithm
    Dim entropy As New Entropy_DrawRect
    Public Sub New()
        labels(2) = "Control entropy values with histogram bins slider"
        desc = "Compute the entropy in an image - a measure of contrast(iness)"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        entropy.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))

        If standaloneTest() Then
            If task.heartBeat Then strOut = "More histogram bins means higher entropy values" + vbCrLf + vbCrLf + "Total entropy = " +
                                             Format(entropy.entropyVal, fmt1) + vbCrLf + entropy.strOut
            setTrueText(strout, 2)
        End If
    End Sub
End Class






Public Class Entropy_Highest : Inherits VB_Algorithm
    Dim entropy As New Entropy_DrawRect
    Public eMaxRect As cv.Rect
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.GridSize.Value = dst2.Width / 10
        labels(2) = "Highest entropy marked with red rectangle"
        desc = "Find the highest entropy section of the color image."
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim entropyMap = New cv.Mat(src.Size(), cv.MatType.CV_32F)
        Dim entropyList(task.gridList.Count - 1) As Single
        Dim maxEntropy As Single = Single.MinValue
        Dim minEntropy As Single = Single.MaxValue

        src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        For Each roi In task.gridList
            entropy.Run(src(roi))
            entropyMap(roi).SetTo(entropy.entropyVal)

            If entropy.entropyVal > maxEntropy Or task.optionsChanged Then
                maxEntropy = entropy.entropyVal
                eMaxRect = roi
            End If
            If entropy.entropyVal < minEntropy Then minEntropy = entropy.entropyVal
            If standaloneTest() Then
                Dim pt = New cv.Point(roi.X, roi.Y)
                setTrueText(Format(entropy.entropyVal, fmt2), pt, 2)
                setTrueText(Format(entropy.entropyVal, fmt2), pt, 3)
            End If
        Next

        dst2 = entropyMap.ConvertScaleAbs(255 / (maxEntropy - minEntropy), minEntropy)
        addw.src2 = src
        addw.Run(dst2)
        dst2 = addw.dst2

        If standaloneTest() Then
            dst2.Rectangle(eMaxRect, 255, task.lineWidth)
            dst3.SetTo(0)
            dst3.Rectangle(eMaxRect, cv.Scalar.White, task.lineWidth)
        End If
        labels(2) = "Lighter = higher entropy. Range: " + Format(minEntropy, "0.0") + " to " + Format(maxEntropy, "0.0")
    End Sub
End Class






Public Class Entropy_FAST : Inherits VB_Algorithm
    Dim fast As New Corners_FAST
    Dim entropy As New Entropy_Highest
    Public Sub New()
        labels = {"", "", "Output of Corners_FAST, input to entropy calculation", "Lighter color is higher entropy, highlight shows highest"}
        desc = "Use FAST markings to add to entropy"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        fast.Run(src)

        entropy.Run(fast.dst2)
        dst2 = entropy.dst2
        dst3 = entropy.dst2
        dst3.Rectangle(entropy.eMaxRect, task.highlightColor, task.lineWidth)
    End Sub
End Class





Public Class Entropy_DrawRect : Inherits VB_Algorithm
    Public entropyVal As Single
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public Sub New()
        desc = "Calculate the entropy in the drawRect when run standaloneTest()"
    End Sub
    Public Function channelEntropy(total As Integer, hist As cv.Mat) As Single
        channelEntropy = 0
        For i = 0 To hist.Rows - 1
            Dim hc = Math.Abs(hist.Get(Of Single)(i))
            If hc <> 0 Then channelEntropy += -(hc / total) * Math.Log10(hc / total)
        Next
        Return channelEntropy
    End Function
    Public Sub RunVB(src as cv.Mat)
        Dim dimensions() = New Integer() {task.histogramBins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 255)}

        If standalone And (task.drawRect.Width = 0 Or task.drawRect.Height = 0) Then
            task.drawRect = New cv.Rect(100, 100, 50, 50) ' arbitrary template to match
        End If
        entropyVal = 0
        Dim input = If(task.drawRect.Width = 0, src, src(task.drawRect))
        strOut = ""
        For i = 0 To input.Channels - 1
            Dim hist As New cv.Mat
            cv.Cv2.CalcHist({input}, {i}, New cv.Mat(), hist, 1, dimensions, ranges)
            histRaw(i) = hist.Clone()
            histNormalized(i) = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

            Dim nextEntropy = channelEntropy(input.Total, histNormalized(i))
            If input.Channels = 1 Then
                strOut += "Entropy X1000 for gray channel " + Format(nextEntropy * 1000, fmt1) + vbCrLf
            Else
                strOut += "Entropy X1000 for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy * 1000, fmt1) + vbCrLf
            End If
            entropyVal += nextEntropy * 1000
        Next
        If standaloneTest() Then
            dst2 = src
            setTrueText(strOut, 3)
        End If
    End Sub
End Class