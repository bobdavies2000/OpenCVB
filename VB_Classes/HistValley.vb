Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports NAudio.Gui
Imports System.Windows.Documents

Public Class HistValley_Basics : Inherits VB_Algorithm
    Dim kalman As New Histogram_Kalman
    Public histogram As New cv.Mat
    Public auto As New OpAuto_Valley
    Public Sub New()
        gOptions.HistBinSlider.Value = 256
        labels = {"", "", "Grayscale histogram - white lines are valleys", ""}
        desc = "Isolate the different levels of gray using the histogram valleys."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        kalman.Run(src)
        dst2 = kalman.dst2
        If firstPass Or task.optionsChanged Then
            histogram = kalman.hist.histogram.Clone
            auto.Run(histogram)
        End If

        If auto.valleyOrder.Count = 0 Then Exit Sub

        For i = 0 To auto.valleyOrder.Count - 1
            Dim entry = auto.valleyOrder.ElementAt(i)
            Dim cClass = CSng(CInt(255 / (i + 1)))
            Dim index = If(i Mod 2, cClass, 255 - cClass)
            For j = entry.Key To entry.Value
                histogram.Set(Of Single)(j, 0, index)
            Next
            Dim col = dst2.Width * entry.Value / task.histogramBins
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth)
        Next

        If src.Type = cv.MatType.CV_32F Then histogram += 1
        cv.Cv2.CalcBackProject({src}, {0}, histogram, dst1, kalman.hist.ranges)
        If dst1.Type <> cv.MatType.CV_8U Then
            dst1.SetTo(0, task.noDepthMask)
            dst1.ConvertTo(dst1, cv.MatType.CV_8U)
        End If

        dst3 = vbPalette(dst1)
        labels(3) = CStr(auto.valleyOrder.Count + 1) + " colors in the back projection"
    End Sub
End Class







Public Class HistValley_Depth : Inherits VB_Algorithm
    Public valley As New HistValley_Basics
    Public valleyOrder As New SortedList(Of Integer, Integer)(New compareAllowIdenticalInteger)
    Public Sub New()
        gOptions.HistBinSlider.Value = 500
        desc = "Find the valleys in the depth histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32F Then src = task.pcSplit(2)
        task.pcSplit(2).SetTo(task.maxZmeters, task.maxDepthMask)
        valley.Run(src)
        dst1 = valley.dst1
        dst2 = valley.dst2
        dst3 = valley.dst3
        valleyOrder = valley.auto.valleyOrder
    End Sub
End Class







Public Class HistValley_LeftRight : Inherits VB_Algorithm
    Dim valley As New HistValley_Lines
    Public Sub New()
        If standalone Then gOptions.displayDst0.Checked = True
        If standalone Then gOptions.displayDst1.Checked = True
        labels = {"Left view histogram", "Right view histogram", "Backprojection left view", "BackProjection right view"}
        desc = "Use the same list of histogram valleys on the left and right images"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        valley.Run(task.leftview)
        dst2 = valley.dst3
        dst0 = valley.dst2

        valley.Run(task.rightview)
        dst3 = valley.dst3
        dst1 = valley.dst2
    End Sub
End Class







Public Class HistValley_Lines : Inherits VB_Algorithm
    Dim lines As New Line_Basics
    Dim valley As New HistValley_Basics
    Public Sub New()
        gOptions.LineType.SelectedItem = "Link4"
        desc = "Insert lines in the color image to help isolate color boundaries"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If task.paused = False Then lines.Run(src)

        src.SetTo(cv.Scalar.Black, lines.dst3)
        valley.Run(src)
        dst2 = valley.dst2
        dst3 = valley.dst3
    End Sub
End Class






Public Class HistValley_Diff : Inherits VB_Algorithm
    Dim diff As New Diff_Basics
    Dim valley As New HistValley_Basics
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        desc = "Compare frame to frame what has changed"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        valley.Run(src)
        dst2 = valley.dst2

        diff.Run(valley.dst3)
        dst3 = diff.dst2
        dst1 = diff.dst3
    End Sub
End Class









Public Class HistValley_EdgeDraw : Inherits VB_Algorithm
    Dim valley As New HistValley_Basics
    Dim edges As New EdgeDraw_Basics
    Public Sub New()
        desc = "Remove edge color in RGB before HistValley_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        edges.Run(src)

        dst1 = src
        dst1.SetTo(cv.Scalar.Black, edges.dst2)

        valley.Run(dst1)
        dst2 = valley.dst2
        dst3 = valley.dst3
    End Sub
End Class






Public Class HistValley_Colors : Inherits VB_Algorithm
    Dim hist As New Histogram_Kalman
    Dim auto As New OpAuto_Valley
    Public Sub New()
        If standalone Then gOptions.HistBinSlider.Value = 256
        If standalone Then findSlider("Desired boundary count").Value = 10
        desc = "Find the histogram valleys for each of the colors."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static splitIndex As Integer
        If heartBeat() Then splitIndex = (splitIndex + 1) Mod 3
        src = src.ExtractChannel(splitIndex)
        hist.hist.plot.backColor = Choose(splitIndex + 1, cv.Scalar.Blue, cv.Scalar.Green, cv.Scalar.Red)

        hist.Run(src)
        dst2 = hist.dst2

        auto.Run(hist.hist.histogram)

        For i = 0 To auto.valleyOrder.Count - 1
            Dim entry = auto.valleyOrder.ElementAt(i)
            Dim cClass = CSng(CInt(255 / (i + 1)))
            Dim index = If(i Mod 2, cClass, 255 - cClass)
            For j = entry.Key To entry.Value
                hist.hist.histogram.Set(Of Single)(j, 0, index)
            Next
            Dim col = dst2.Width * entry.Value / task.histogramBins
            dst2.Line(New cv.Point(col, 0), New cv.Point(col, dst2.Height), cv.Scalar.White, task.lineWidth)
        Next
    End Sub
End Class







Public Class HistValley_Simple : Inherits VB_Algorithm
    Dim trends As New SLR_Trends
    Public kalman As New Kalman_Basics
    Public depthRegions As New List(Of Integer)
    Public plot As New Plot_Histogram
    Public Sub New()
        desc = "Identify ranges by marking the depth histogram entries from valley to valley"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        trends.Run(src)

        If kalman.kInput.Length <> task.histogramBins Then ReDim kalman.kInput(task.histogramBins - 1)
        kalman.kInput = trends.resultingValues.ToArray
        kalman.Run(src)

        dst2.SetTo(cv.Scalar.Black)
        Dim barWidth As Single = dst2.Width / trends.resultingValues.Count
        Dim colorIndex As Integer
        Dim color = task.scalarColors(colorIndex Mod 256)
        Dim vals() = {-1, -1, -1}
        For i = 0 To kalman.kOutput.Count - 1
            Dim h = dst2.Height - kalman.kOutput(i)
            vals(0) = vals(1)
            vals(1) = vals(2)
            vals(2) = h
            If vals(0) >= 0 Then
                If vals(0) > vals(1) And vals(2) > vals(1) Then
                    colorIndex += 1
                    color = task.scalarColors(colorIndex Mod 256)
                End If
            End If
            cv.Cv2.Rectangle(dst2, New cv.Rect(i * barWidth, dst2.Height - h, barWidth, h), color, -1)
            depthRegions.Add(colorIndex)
        Next

        Dim lastPoint As cv.Point = trends.resultingPoints(0)
        For i = 1 To trends.resultingPoints.Count - 1
            Dim p1 = trends.resultingPoints(i)
            dst2.Line(lastPoint, p1, cv.Scalar.Yellow, task.lineWidth + 1, task.lineType)
            lastPoint = p1
        Next
        labels(2) = "Depth regions between 0 and " + CStr(CInt(task.maxZmeters + 1)) + " meters"
    End Sub
End Class