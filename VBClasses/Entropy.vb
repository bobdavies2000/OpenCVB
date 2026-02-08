Imports cv = OpenCvSharp
' http://areshopencvb.blogspot.com/2011/12/computing-entropy-of-image.html
Namespace VBClasses
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
            If r.X + r.Width >= tsk.workRes.Width Then r.X = tsk.workRes.Width - r.Width - 1
            If r.Y + r.Height >= tsk.workRes.Height Then r.Y = tsk.workRes.Height - r.Height - 1
            Return r
        End Function
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim stdSize = 30
            If tsk.drawRect = New cv.Rect Then
                tsk.drawRect = New cv.Rect(30, 30, stdSize, stdSize) ' arbitrary rectangle
            End If
            If tsk.mouseClickFlag Then
                tsk.drawRect = validatePreserve(New cv.Rect(tsk.clickPoint.X, tsk.clickPoint.Y, stdSize, stdSize))
            End If
            tsk.drawRect = ValidateRect(tsk.drawRect)
            If src.Channels() = 3 Then
                entropy.Run(src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)(tsk.drawRect))
            Else
                entropy.Run(src(tsk.drawRect))
            End If
            dst2 = entropy.dst2
            dst2.Rectangle(tsk.drawRect, white, tsk.lineWidth)
            If tsk.heartBeat Then strOut = "Click anywhere to measure the entropy with rect(pt.x, pt.y, " +
                                             CStr(stdSize) + ", " + CStr(stdSize) + ")" + vbCrLf + vbCrLf + "Total entropy = " +
                                             Format(entropy.entropyVal, fmt1) + vbCrLf + entropy.strOut
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class Entropy_Highest : Inherits TaskParent
        Dim entropy As New Entropy_Rectangle
        Public eMaxRect As cv.Rect
        Public Sub New()
            dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_32F, 0)
            If standalone Then
                Dim val As Integer = dst2.Width / 10
                If tsk.gOptions.GridSlider.Maximum < val Then tsk.gOptions.GridSlider.Maximum = val
                tsk.gOptions.GridSlider.Value = dst2.Width \ 10
            End If
            desc = "Find the highest entropy section of the color image."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim entropyList(tsk.gridRects.Count - 1) As Single
            Dim maxEntropy As Single = Single.MinValue
            Dim minEntropy As Single = Single.MaxValue
            trueData.Clear()

            src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            dst1.SetTo(0)
            For Each roi In tsk.gridRects
                If roi.Width = roi.Height Then
                    entropy.Run(src(roi))
                    dst1(roi).SetTo(entropy.entropyVal)

                    If entropy.entropyVal > maxEntropy Or tsk.optionsChanged Then
                        maxEntropy = entropy.entropyVal
                        eMaxRect = roi
                    End If
                    If entropy.entropyVal < minEntropy Then minEntropy = entropy.entropyVal
                    If standaloneTest() And tsk.brickSize > 16 Then
                        Dim pt = New cv.Point(roi.X, roi.Y)
                        SetTrueText(Format(entropy.entropyVal, fmt2), pt, 2)
                        SetTrueText(Format(entropy.entropyVal, fmt2), pt, 3)
                    End If
                End If
            Next

            dst2 = dst1.ConvertScaleAbs(255 / (maxEntropy - minEntropy), minEntropy)
            dst2 = ShowAddweighted(src, dst2, labels(3))

            If standaloneTest() Then
                dst2.Rectangle(eMaxRect, 255, tsk.lineWidth)
                dst3.SetTo(0)
                dst3.Rectangle(eMaxRect, white, tsk.lineWidth)
            End If
            labels(2) = "Lighter = higher entropy. Range: " + Format(minEntropy, "0.0") + " to " + Format(maxEntropy, "0.0")
        End Sub
    End Class






    Public Class NR_Entropy_FAST : Inherits TaskParent
        Dim fast As New Corners_Basics
        Dim entropy As New Entropy_Highest
        Public Sub New()
            labels = {"", "", "Output of Corners_FAST, input to entropy calculation", "Lighter color is higher entropy, highlight shows highest"}
            desc = "Use FAST markings to add to entropy"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            fast.Run(src)

            entropy.Run(fast.dst2)
            dst2 = entropy.dst2
            dst3 = entropy.dst2
            dst3.Rectangle(entropy.eMaxRect, tsk.highlight, tsk.lineWidth)
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
        Public Overrides Sub RunAlg(src As cv.Mat)
            Dim dimensions() = New Integer() {tsk.histogramBins}
            If src.Channels() <> 1 Then src = src.CvtColor(cv.ColorConversionCodes.BGR2GRAY)

            Dim mm = GetMinMax(src)
            Dim ranges() = New cv.Rangef() {New cv.Rangef(mm.minVal, mm.maxVal)}
            If mm.minVal = mm.maxVal Then ranges = New cv.Rangef() {New cv.Rangef(0, 255)}

            If standalone Then
                If tsk.drawRect.Width = 0 Or tsk.drawRect.Height = 0 Then
                    tsk.drawRect = New cv.Rect(10, 10, 50, 50) ' arbitrary template to match
                End If
                src = src(tsk.drawRect)
            End If
            Dim hist As New cv.Mat
            cv.Cv2.CalcHist({src}, {0}, New cv.Mat(), hist, 1, dimensions, ranges)
            Dim histNormalized = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

            entropyVal = channelEntropy(src.Total, histNormalized) * 1000
            strOut = "Entropy X1000 " + Format(entropyVal, fmt1) + vbCrLf
            dst2 = src
            dst2.Rectangle(tsk.drawRect, white, tsk.lineWidth)
            dst3 = src
            SetTrueText(strOut, 3)
        End Sub
    End Class






    Public Class NR_Entropy_SubDivisions : Inherits TaskParent
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

            dst0 = New cv.Mat(dst0.Size, cv.MatType.CV_8U, 0)
            desc = "Find the highest entropy in each quadrant"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            If tsk.firstPass Then
                For Each roi In tsk.gridRects
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
                vbc.DrawLine(dst0, p1, p2, white)
                p1 = New cv.Point(0, dst2.Height * 2 / 3)
                p2 = New cv.Point(dst2.Width, dst2.Height * 2 / 3)
                vbc.DrawLine(dst0, p1, p2, white)
                p1 = New cv.Point(dst2.Width / 3, 0)
                p2 = New cv.Point(dst2.Width / 3, dst2.Height)
                vbc.DrawLine(dst0, p1, p2, white)
                p1 = New cv.Point(dst2.Width * 2 / 3, 0)
                p2 = New cv.Point(dst2.Width * 2 / 3, dst2.Height)
                vbc.DrawLine(dst0, p1, p2, white)
            End If

            dst2 = tsk.color.Clone
            For i = 0 To subDivisionCount - 1
                entropies(i).Clear()
                eROI(i).Clear()
            Next

            dst1 = tsk.grayStable.Clone
            Dim dimensions() = New Integer() {tsk.histogramBins}
            Dim ranges() = New cv.Rangef() {New cv.Rangef(0, 255)}
            Dim hist As New cv.Mat
            For i = 0 To tsk.gridRects.Count - 1
                Dim gr = tsk.gridRects(i)
                cv.Cv2.CalcHist({dst1(gr)}, {0}, New cv.Mat(), hist, 1, dimensions, ranges)
                hist = hist.Normalize(0, hist.Rows, cv.NormTypes.MinMax)

                Dim nextEntropy = entropy.channelEntropy(dst1(gr).Total, hist) * 1000

                entropies(subDivisions(i)).Add(nextEntropy)
                eROI(subDivisions(i)).Add(gr)
            Next

            Dim str = If(tsk.toggleOn, "minimum", "maximum")
            labels(3) = "The " + str + " entropy values in each subdivision"
            For i = 0 To entropies.Count - 1
                Dim val = If(tsk.toggleOn, entropies(i).Min, entropies(i).Max)
                Dim index = entropies(i).IndexOf(val)
                Dim roi = eROI(i)(index)
                dst2.Rectangle(roi, white)
                If standaloneTest() Then SetTrueText(Format(entropies(i)(index), fmt2), roi.TopLeft, 3)
            Next

            dst2.SetTo(white, dst0)
            dst3.SetTo(white, dst0)
        End Sub
    End Class






    Public Class NR_Entropy_BinaryImage : Inherits TaskParent
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
End Namespace