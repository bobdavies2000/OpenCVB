Imports OpenCvSharp.Cv2 : Imports OpenCvSharp : Imports cv = OpenCvSharp
Namespace VBClasses
    ' http://areshopencvb.blogspot.com/2011/12/computing-entropy-of-image.html
    Public Class Entropy_Basics : Inherits TaskParent
        Dim entropy As New Entropy_Rectangle
        Public Sub New()
            labels(2) = "Control entropy values with histogram bins slider"
            desc = "Compute the entropy in an image - a measure of contrast(iness)"
        End Sub
        Private Shared Function validatePreserve(ByVal r As cv.Rect) As cv.Rect
            If r.Width <= 0 Then r.Width = 1
            If r.Height <= 0 Then r.Height = 1
            If r.X < 0 Then r.X = 0
            If r.Y < 0 Then r.Y = 0
            If r.X + r.Width >= task.workRes.Width Then r.X = task.workRes.Width - r.Width - 1
            If r.Y + r.Height >= task.workRes.Height Then r.Y = task.workRes.Height - r.Height - 1
            Return r
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim stdSize = 30
            If task.drawRect = New cv.Rect Then
                task.drawRect = New cv.Rect(30, 30, stdSize, stdSize) ' arbitrary rectangle
            End If
            If task.mouseClickFlag Then
                task.drawRect = validatePreserve(New cv.Rect(task.clickPoint.X, task.clickPoint.Y, stdSize, stdSize))
            End If
            task.drawRect = ValidateRect(task.drawRect)
            If src.Channels() <> 1 Then
                entropy.Run(task.gray(task.drawRect))
            Else
                entropy.Run(src(task.drawRect))
            End If
            dst2 = entropy.dst2
            Rectangle(dst2, task.drawRect, white, task.lineWidth)
            If task.heartBeat Then strOut = "Click anywhere to measure the entropy with cv.Rect(pt.x, pt.y, " +
                                                 CStr(stdSize) + ", " + CStr(stdSize) + ")" + vbCrLf + vbCrLf + "Total entropy = " +
                                                 entropy.entropyVal.ToString(fmt1) + vbCrLf + entropy.strOut
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class Entropy_Rectangle : Inherits TaskParent
        Public entropyVal As Single
        Public Sub New()
            desc = "Calculate the entropy in the drawRect when run standalone"
        End Sub
        Public Shared Function channelEntropy(total As Integer, hist As Mat) As Single
            channelEntropy = 0
            For i = 0 To hist.Rows - 1
                Dim hc = Math.Abs(hist.Get(Of Single)(i))
                If hc <> 0 Then channelEntropy += -(hc / total) * Math.Log10(hc / total)
            Next
            Return channelEntropy
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim dimensions() = New Integer() {task.histogramBins}
            If src.Channels() <> 1 Then src = task.gray

            Dim mm = GetMinMax(src)
            Dim ranges() = New Rangef() {New Rangef(mm.minVal, mm.maxVal)}
            If mm.minVal = mm.maxVal Then ranges = New Rangef() {New Rangef(0, 255)}

            If standalone Then
                If task.drawRect.Width = 0 Or task.drawRect.Height = 0 Then
                    task.drawRect = New cv.Rect(10, 10, 50, 50) ' arbitrary template to match
                End If
                src = src(task.drawRect)
            End If
            Dim hist As New Mat
            CalcHist({src}, {0}, New Mat(), hist, 1, dimensions, ranges)
            Dim histNormalized As New Mat
            Normalize(hist, histNormalized, 0, hist.Rows, NormTypes.MinMax)

            entropyVal = channelEntropy(src.Total, histNormalized) * 1000
            strOut = "Entropy X1000 " + entropyVal.ToString(fmt1) + vbCrLf
            dst2 = src
            Rectangle(dst2, task.drawRect, white, task.lineWidth)
            dst3 = src
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class XR_Entropy_SubDivisions : Inherits TaskParent
        Dim entropy As New Entropy_Rectangle
        Dim entropies As New List(Of List(Of Single))
        Dim eROI As New List(Of List(Of cv.Rect))
        Public subDivisions As New List(Of Integer)
        Public subDivisionCount As Integer = 9
        Public Sub New()
            labels(2) = "Highlighted rectangles are the top entropy in each of the 9 subdivisions."
            For i = 0 To subDivisionCount - 1
                entropies.Add(New List(Of Single)) ' 4 quadrants
                eROI.Add(New List(Of cv.Rect)) ' 4 quadrants
            Next

            dst0 = New Mat(dst0.Size, MatType.CV_8U, 0)
            desc = "Find the highest entropy in each quadrant"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If task.firstPass Then
                For Each roi In task.gridRects
                    Dim xSub = roi.X + roi.Width
                    Dim ySub = roi.Y + roi.Height
                    If ySub <= dst2.Height / 3 Then
                        If xSub <= dst2.Width / 3 Then subDivisions.Add(0)
                        If xSub >= dst2.Width / 3 And xSub <= dst2.Width * 2 / 3 Then subDivisions.Add(1)
                        If xSub > dst2.Width * 2 / 3 Then subDivisions.Add(2)
                    End If
                    If ySub > dst2.Height / 3 And ySub <= dst2.Height * 2 / 3 Then
                        If xSub <= dst2.Width / 3 Then subDivisions.Add(3)
                        If xSub >= dst2.Width / 3 And xSub <= dst2.Width * 2 / 3 Then subDivisions.Add(4)
                        If xSub > dst2.Width * 2 / 3 Then subDivisions.Add(5)
                    End If
                    If ySub > dst2.Height * 2 / 3 Then
                        If xSub <= dst2.Width / 3 Then subDivisions.Add(6)
                        If xSub >= dst2.Width / 3 And xSub <= dst2.Width * 2 / 3 Then subDivisions.Add(7)
                        If xSub > dst2.Width * 2 / 3 Then subDivisions.Add(8)
                    End If
                Next

                Dim p1 = New cv.Point(0, dst2.Height / 3)
                Dim p2 = New cv.Point(dst2.Width, dst2.Height / 3)
                Line(dst0, p1, p2, white, task.lineWidth, task.lineType)
                p1 = New cv.Point(0, dst2.Height * 2 / 3)
                p2 = New cv.Point(dst2.Width, dst2.Height * 2 / 3)
                Line(dst0, p1, p2, white, task.lineWidth, task.lineType)
                p1 = New cv.Point(dst2.Width / 3, 0)
                p2 = New cv.Point(dst2.Width / 3, dst2.Height)
                Line(dst0, p1, p2, white, task.lineWidth, task.lineType)
                p1 = New cv.Point(dst2.Width * 2 / 3, 0)
                p2 = New cv.Point(dst2.Width * 2 / 3, dst2.Height)
                Line(dst0, p1, p2, white, task.lineWidth, task.lineType)
            End If

            dst2 = task.color.Clone
            For i = 0 To subDivisionCount - 1
                entropies(i).Clear()
                eROI(i).Clear()
            Next

            dst1 = task.gray.Clone
            Dim dimensions() = New Integer() {task.histogramBins}
            Dim ranges() = New Rangef() {New Rangef(0, 255)}
            Dim hist As New Mat
            For i = 0 To task.gridRects.Count - 1
                Dim r = task.gridRects(i)
                CalcHist({dst1(r)}, {0}, New Mat(), hist, 1, dimensions, ranges)
                Normalize(hist, hist, 0, hist.Rows, NormTypes.MinMax)

                Dim nextEntropy = Entropy_Rectangle.channelEntropy(dst1(r).Total, hist) * 1000

                entropies(subDivisions(i)).Add(nextEntropy)
                eROI(subDivisions(i)).Add(r)
            Next

            Dim str = If(task.toggleOn, "minimum", "maximum")
            labels(3) = "The " + str + " entropy values in each subdivision"
            For i = 0 To entropies.Count - 1
                Dim val = If(task.toggleOn, entropies(i).Min, entropies(i).Max)
                Dim index = entropies(i).IndexOf(val)
                Dim roi = eROI(i)(index)
                Rectangle(dst2, roi, white)
                If standaloneTest() Then SetTrueText(entropies(i)(index).ToString(fmt2), roi.TopLeft, 3)
            Next

            dst2.SetTo(white, dst0)
            dst3.SetTo(white, dst0)
        End Sub
    End Class






    Public Class XR_Entropy_BinaryImage : Inherits TaskParent
        Dim binary As New Binarize_Simple
        Dim entropy As New Entropy_Basics
        Public Sub New()
            desc = "Measure entropy in a binary image"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            binary.Run(src)
            dst2 = binary.dst2
            labels(2) = binary.labels(2)

            entropy.Run(dst2)
            SetTrueText(entropy.strOut, 3)
        End Sub
    End Class




    Public Class Entropy_Highest : Inherits TaskParent
        Dim entropy As New Entropy_Rectangle
        Public Sub New()
            If standalone Then setLargeGridSize()
            dst3 = New Mat(dst3.Size, MatType.CV_32F, 0)
            labels(3) = "High entropy = busy, detailed, noisy. Low entropy = smooth"
            desc = "What is the entropy for each cell using the task.lines.dst3 as input"
        End Sub
        Public Shared Sub setLargeGridSize()
            Dim val As Integer = task.workRes.Width / 10
            If task.gOptions.GridSlider.Maximum < val Then task.gOptions.GridSlider.Maximum = val
            task.gOptions.GridSlider.Value = task.workRes.Width \ 10
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If src.Channels <> 1 Then src = task.gray

            dst3.SetTo(0)
            Dim entropies As New List(Of Single)
            For Each roi In task.gridRects
                If roi.Width <> roi.Height Then Continue For
                entropy.Run(src(roi))
                Dim eVal = entropy.entropyVal
                entropies.Add(eVal)
                dst3(roi).SetTo(eVal)

                If task.gridWH > 16 Then SetTrueText(eVal.ToString("#0.00"), roi.TopLeft, 2)
            Next

            ConvertScaleAbs(dst3, dst2, 255 / (entropies.Max - entropies.Min), entropies.Min)
            dst2 = ShowAddweighted(src, dst2, labels(1))

            labels(2) = "Lighter = higher entropy. Range: " + entropies.Max.ToString("0.0") + " to " + entropies.Min.ToString("0.0")
        End Sub
    End Class

End Namespace