Imports cv = OpenCvSharp
' http://areshopencv.blogspot.com/2011/12/computing-entropy-of-image.html
Public Class Entropy_Basics : Inherits VB_Algorithm
    Dim entropy As New Entropy_Rectangle
    Public Sub New()
        labels(2) = "Control entropy values with histogram bins slider"
        desc = "Compute the entropy in an image - a measure of contrast(iness)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim stdSize = 50
        If task.drawRect = New cv.Rect Then
            task.drawRect = New cv.Rect(50, 50, stdSize, stdSize) ' arbitrary rectangle
        End If
        If task.mouseClickFlag Then
            task.drawRect = validatePreserve(New cv.Rect(task.clickPoint.X, task.clickPoint.Y, stdSize, stdSize))
        End If
        If src.Channels = 3 Then
            entropy.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)(task.drawRect))
        Else
            entropy.Run(src(task.drawRect))
        End If
        dst2 = entropy.dst2
        dst2.Rectangle(task.drawRect, cv.Scalar.White, task.lineWidth)
        If task.heartBeat Then strOut = "Click anywhere to measure the entropy with rect(pt.x, pt.y, " +
                                         CStr(stdSize) + ", " + CStr(stdSize) + ")" + vbCrLf + vbCrLf + "Total entropy = " +
                                         Format(entropy.entropyVal, fmt1) + vbCrLf + entropy.strOut
        setTrueText(strOut, 3)
    End Sub
End Class






Public Class Entropy_Highest : Inherits VB_Algorithm
    Dim entropy As New Entropy_Rectangle
    Public eMaxRect As cv.Rect
    Dim addw As New AddWeighted_Basics
    Public Sub New()
        If standaloneTest() Then gOptions.GridSize.Value = dst2.Width / 10
        labels(2) = "Highest entropy marked with red rectangle"
        desc = "Find the highest entropy section of the color image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
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
    Public Sub RunVB(src As cv.Mat)
        fast.Run(src)

        entropy.Run(fast.dst2)
        dst2 = entropy.dst2
        dst3 = entropy.dst2
        dst3.Rectangle(entropy.eMaxRect, task.highlightColor, task.lineWidth)
    End Sub
End Class





Public Class Entropy_Rectangle : Inherits VB_Algorithm
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
    Public Sub RunVB(src As cv.Mat)
        Dim dimensions() = New Integer() {task.histogramBins}
        If src.Channels <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

        Dim mm = vbMinMax(src)
        Dim ranges() = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
        If mm.minVal = mm.maxVal Then ranges = New cv.Rangef() {New cv.Rangef(0, 255)}

        If standalone Then
            If task.drawRect.Width = 0 Or task.drawRect.Height = 0 Then
                task.drawRect = New cv.Rect(100, 100, 50, 50) ' arbitrary template to match
            End If
            src = src(task.drawRect)
        End If
        Dim hist As New cv.Mat
        cv.Cv2.CalcHist({src}, {0}, New cv.Mat(), hist, 1, dimensions, ranges)
        Dim histNormalized = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

        entropyVal = channelEntropy(src.Total, histNormalized) * 1000
        strOut = "Entropy X1000 " + Format(entropyVal, fmt1) + vbCrLf
        dst2 = src
        dst2.Rectangle(task.drawRect, cv.Scalar.White, task.lineWidth)
        dst3 = src
        setTrueText(strOut, 3)
    End Sub
End Class






Public Class Entropy_SubDivisions : Inherits VB_Algorithm
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
    Public Sub RunVB(src As cv.Mat)
        dst2 = task.color.Clone
        For i = 0 To task.subDivisionCount - 1
            entropies(i).Clear()
            eROI(i).Clear()
        Next

        dst1 = If(src.Channels = 1, src, src.CvtColor(cv.ColorConversionCodes.BGR2GRAY))
        Dim dimensions() = New Integer() {task.histogramBins}
        Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 255)}
        Dim hist As New cv.Mat
        For i = 0 To task.gridList.Count - 1
            Dim roi = task.gridList(i)
            cv.Cv2.CalcHist({dst1(roi)}, {0}, New cv.Mat(), hist, 1, dimensions, ranges)
            hist = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

            Dim nextEntropy = entropy.channelEntropy(dst1(roi).Total, hist) * 1000

            entropies(task.subDivisions(i)).Add(nextEntropy)
            eROI(task.subDivisions(i)).Add(roi)
            If standaloneTest() Then setTrueText(Format(nextEntropy, fmt2), New cv.Point(roi.X, roi.Y), 3)
        Next

        roiList.Clear()
        For i = 0 To task.subDivisionCount - 1
            Dim eList = entropies(i)
            Dim maxEntropy = eList.Max
            Dim roi = eROI(i)(eList.IndexOf(maxEntropy))
            roiList.Add(roi)
            dst2.Rectangle(roi, cv.Scalar.White, task.lineWidth, task.lineType)
        Next

        Dim p1 = New cv.Point(0, dst2.Height / 3)
        Dim p2 = New cv.Point(dst2.Width, dst2.Height / 3)
        dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
        p1 = New cv.Point(0, dst2.Height * 2 / 3)
        p2 = New cv.Point(dst2.Width, dst2.Height * 2 / 3)
        dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
        p1 = New cv.Point(dst2.Width / 3, 0)
        p2 = New cv.Point(dst2.Width / 3, dst2.Height)
        dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
        p1 = New cv.Point(dst2.Width * 2 / 3, 0)
        p2 = New cv.Point(dst2.Width * 2 / 3, dst2.Height)
        dst2.Line(p1, p2, cv.Scalar.White, task.lineWidth, task.lineType)
    End Sub
End Class






Public Class Entropy_BinaryImage : Inherits VB_Algorithm
    Dim binary As New Binarize_Simple
    Dim entropy As New Entropy_Basics
    Public Sub New()
        desc = "Measure entropy in a binary image"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        binary.Run(src)
        dst2 = binary.dst2
        labels(2) = binary.labels(2)

        entropy.Run(dst2)
        setTrueText(entropy.strOut, 3)
    End Sub
End Class
