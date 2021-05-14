Imports cv = OpenCvSharp
' http://areshopencv.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics : Inherits VBparent
    Dim simple As New Entropy_Simple
    Public entropy As Single
    Public Sub New()
        label1 = "Control entropy values with histogram bins slider"
        task.desc = "Compute the entropy in an image - a measure of contrast(iness)"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        simple.run(src)
        entropy = 0
        Dim entropyChannels As String = ""
        For i = 0 To src.Channels - 1
            Dim nextEntropy = simple.channelEntropy(src.Total, simple.histNormalized(i))
            entropyChannels += "Entropy for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy, "0.00") + ", "
            entropy += nextEntropy
        Next
        If standalone Or task.intermediateName = caller Then
            Static flow = New Font_FlowText()
            flow.msgs.Add("Entropy total = " + Format(entropy, "0.00") + " - " + entropyChannels)
            flow.Run(Nothing)
        End If
    End Sub
End Class






Public Class Entropy_Highest : Inherits VBparent
    Dim entropyCalc as New Entropy_Simple
    Public grid As New Thread_Grid
    Public eMaxRect As cv.Rect
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        findSlider("ThreadGrid Width").Value = 64
        findSlider("ThreadGrid Height").Value = 80
        label1 = "Highest entropy marked with red rectangle"
        task.desc = "Find the highest entropy section of the color image."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        grid.Run(Nothing)

        Dim entropyMap = New cv.Mat(src.Size(), cv.MatType.CV_32F)
        Dim entropyList(grid.roiList.Count - 1) As Single
        Dim maxEntropy As Single = Single.MinValue
        Dim minEntropy As Single = Single.MaxValue
        For Each roi In grid.roiList
            entropyCalc.Run(src(roi))
            entropyMap(roi).SetTo(entropyCalc.entropy)

            If entropyCalc.entropy > maxEntropy Then
                maxEntropy = entropyCalc.entropy
                eMaxRect = roi
            End If
            If entropyCalc.entropy < minEntropy Then minEntropy = entropyCalc.entropy
        Next

        dst2 = entropyMap.ConvertScaleAbs(255 / (maxEntropy - minEntropy), minEntropy)
        If src.Channels = 3 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
        addw.src2 = src
        addw.Run(dst2)
        dst2 = addw.dst1

        Dim tmp = entropyMap.ConvertScaleAbs(255 / (maxEntropy - minEntropy))
        cv.Cv2.MinMaxLoc(tmp, minval, maxval)

        dst1 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If standalone Or task.intermediateName = caller Then dst1.Rectangle(eMaxRect, cv.Scalar.Red, 4)
        label2 = "Lighter = higher entropy. Range: " + Format(minEntropy, "0.0") + " to " + Format(maxEntropy, "0.0")
    End Sub
End Class






Public Class Entropy_FAST : Inherits VBparent
    Dim fast As New FAST_Basics
    Dim entropy as New Entropy_Highest
    Public Sub New()
        label1 = "Output of Fast_Basics, input to entropy calculation"
        label2 = "Lighter color is higher entropy, Red marks highest"
        task.desc = "Use FAST markings to add to entropy"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        fast.Run(src)

        entropy.Run(fast.dst1)
        dst1 = entropy.dst1
        dst2 = entropy.dst2
        dst2.Rectangle(entropy.eMaxRect, cv.Scalar.Red, 4)
    End Sub
End Class





' This algorithm is different and does not inherit from VBParent class.  It is used to reduce the memory load when running MT algorithms above.
Public Class Entropy_Simple : Inherits VBparent
    Public entropy As Single
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public Function channelEntropy(total As Integer, hist As cv.Mat) As Single
        channelEntropy = 0
        For i = 0 To hist.Rows - 1
            Dim hc = Math.Abs(hist.Get(Of Single)(i))
            If hc <> 0 Then channelEntropy += -(hc / total) * Math.Log10(hc / total)
        Next
        Return channelEntropy
    End Function
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Dim dimensions() = New Integer() {task.histogramBins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 255)}

        entropy = 0
        Dim entropyChannels As String = ""
        For i = 0 To src.Channels - 1
            Dim hist As New cv.Mat
            cv.Cv2.CalcHist(New cv.Mat() {src}, New Integer() {i}, New cv.Mat(), hist, 1, dimensions, ranges)
            histRaw(i) = hist.Clone()
            histNormalized(i) = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

            Dim nextEntropy = channelEntropy(src.Total, histNormalized(i))
            entropyChannels += "Entropy for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy, "0.00") + ", "
            entropy += nextEntropy
        Next
    End Sub
End Class

