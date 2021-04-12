Imports cv = OpenCvSharp
' http://areshopencv.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics
    Inherits VBparent
    Dim flow As Font_FlowText
    Dim simple = New Entropy_Simple
    Public entropy As Single
    Public Sub New()
        initParent()
        flow = New Font_FlowText()

        If findfrm(caller + " Slider Options") Is Nothing Then
            sliders.Setup(caller)
            sliders.setupTrackBar(0, "Number of Bins", 0, 100, 50)
        End If

        task.desc = "Compute the entropy in an image - a measure of contrast(iness)"
		' task.rank = 1
    End Sub
    Public Sub Run()
		If task.intermediateReview = caller Then task.intermediateObject = Me
        Static binSlider = findSlider("Number of Bins")
        simple.bins = binSlider.Value
        simple.run(src)
        entropy = 0
        Dim entropyChannels As String = ""
        For i = 0 To src.Channels - 1
            Dim nextEntropy = simple.channelEntropy(src.Total, simple.histNormalized(i))
            entropyChannels += "Entropy for " + Choose(i + 1, "Red", "Green", "Blue") + " " + Format(nextEntropy, "0.00") + ", "
            entropy += nextEntropy
        Next
        If standalone or task.intermediateReview = caller Then
            flow.msgs.Add("Entropy total = " + Format(entropy, "0.00") + " - " + entropyChannels)
            flow.Run()
        End If
    End Sub
End Class






Public Class Entropy_Highest
    Inherits VBparent
    Dim entropyCalc As Entropy_Simple
    Public grid As Thread_Grid
    Public eMaxRect As cv.Rect
    Dim addw As AddWeighted_Basics
    Public Sub New()
        initParent()

        addw = New AddWeighted_Basics

        grid = New Thread_Grid
        Static gridWidthSlider = findSlider("ThreadGrid Width")
        Static gridHeightSlider = findSlider("ThreadGrid Height")
        gridWidthSlider.Value = 64
        gridHeightSlider.Value = 80

        entropyCalc = New Entropy_Simple

        label1 = "Highest entropy marked with red rectangle"
        task.desc = "Find the highest entropy section of the color image."
		' task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        grid.Run()

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
        addw.src = dst2
        addw.src2 = src
        addw.Run()
        dst2 = addw.dst1

        Dim tmp = entropyMap.ConvertScaleAbs(255 / (maxEntropy - minEntropy))
        cv.Cv2.MinMaxLoc(tmp, minval, maxval)

        dst1 = src.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        dst2 = dst2.CvtColor(cv.ColorConversionCodes.GRAY2BGR)
        If standalone Or task.intermediateReview = caller Then dst1.Rectangle(eMaxRect, cv.Scalar.Red, 4)
        label2 = "Lighter = higher entropy. Range: " + Format(minEntropy, "0.0") + " to " + Format(maxEntropy, "0.0")
    End Sub
End Class






Public Class Entropy_FAST
    Inherits VBparent
    Dim fast As FAST_Basics
    Dim entropy As Entropy_Highest
    Public Sub New()
        initParent()
        fast = New FAST_Basics()
        entropy = New Entropy_Highest()

        label1 = "Output of Fast_Basics, input to entropy calculation"
        label2 = "Lighter color is higher entropy, Red marks highest"
        task.desc = "Use FAST markings to add to entropy"
		' task.rank = 1
    End Sub
    Public Sub Run()
        If task.intermediateReview = caller Then task.intermediateObject = Me
        fast.src = src
        fast.Run()

        entropy.src = fast.dst1
        entropy.Run()
        dst1 = entropy.dst1
        dst2 = entropy.dst2
        dst2.Rectangle(entropy.eMaxRect, cv.Scalar.Red, 4)
    End Sub
End Class





' This algorithm is different and does not inherit from VBParent class.  It is used to reduce the memory load when running MT algorithms above.
Public Class Entropy_Simple
    Public entropy As Single
    Public histRaw(3 - 1) As cv.Mat
    Public histNormalized(3 - 1) As cv.Mat
    Public bins As Integer = 256
    Public minRange As Integer = 0
    Public maxRange As Integer = 255
    Public Function channelEntropy(total As Integer, hist As cv.Mat) As Single
        channelEntropy = 0
        For i = 0 To hist.Rows - 1
            Dim hc = Math.Abs(hist.Get(Of Single)(i))
            If hc <> 0 Then channelEntropy += -(hc / total) * Math.Log10(hc / total)
        Next
        Return channelEntropy
    End Function
    Public Sub Run(src As cv.Mat)
        Dim dimensions() = New Integer() {bins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(minRange, maxRange)}

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

