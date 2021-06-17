Imports cv = OpenCvSharp
Public Class Proximity_Basics : Inherits VBparent
    Dim kalman As New Kalman_Basics
    Public hist As New Histogram_Depth
    Public ranges As New List(Of cv.Point)
    Public barValues As New List(Of Integer)
    Public barDepth As New List(Of Integer)
    Public barHeight As New List(Of Integer)
    Public rangeCounts As New List(Of Integer)
    Public Sub New()
        label1 = "Histogram clustered by valleys and smoothed"
        task.desc = "Identify valleys in the Depth histogram."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f
        hist.Run(src)
        If kalman.kInput.Length <> hist.plotHist.hist.Rows Then ReDim kalman.kInput(hist.plotHist.hist.Rows - 1)
        For i = 0 To hist.plotHist.hist.Rows - 1
            kalman.kInput(i) = hist.plotHist.hist.Get(Of Single)(i, 0)
        Next
        kalman.Run(src)
        Dim histogram = hist.plotHist.hist
        For i = 0 To histogram.Rows - 1
            histogram.Set(Of Single)(i, 0, kalman.kOutput(i))
        Next

        Dim depthIncr = task.maxDepth / task.histogramBins ' each bar represents this number of millimeters
        Dim startDepth = 1
        ranges.Clear()
        rangeCounts.Clear()

        Dim pointcount As Integer
        barValues.Clear()
        barDepth.Clear()
        barHeight.Clear()
        Dim barCurrent As Integer = 1
        For i = 0 To kalman.kOutput.Length - 1
            If i >= 2 And i <= kalman.kOutput.Length - 3 Then
                Dim prev2 = kalman.kOutput(i - 2)
                Dim prev = kalman.kOutput(i - 1)
                Dim curr = kalman.kOutput(i)
                Dim post = kalman.kOutput(i + 1)
                Dim post2 = kalman.kOutput(i + 2)
                If curr < 100 Then curr = 0 ' too small to worry about plotting...
                pointcount += curr
                If (prev2 > 1 And prev > 1 And curr > 1 And post > 1 And post2 > 1) Or curr = 0 Then
                    If (curr < (prev + prev2) / 2 And curr < (post + post2) / 2 And i * depthIncr > startDepth + depthIncr) Or curr = 0 Then
                        If pointcount > 1000 Then
                            barCurrent += 1
                            ranges.Add(New cv.Point(startDepth, i * depthIncr))
                            rangeCounts.Add(pointcount)
                            pointcount = 0
                        End If
                        startDepth = i * depthIncr + 1
                    End If
                End If
            End If
            barValues.Add(barCurrent)
            barDepth.Add(depthIncr * i)
        Next
        If ranges.Count > 0 Then
            ranges.Add(New cv.Point(ranges(ranges.Count - 1).Y, CInt(task.maxZ * 1000)))
        Else
            ranges.Add(New cv.Point(0, CInt(task.maxZ * 1000)))
        End If
        rangeCounts.Add(pointcount)

        dst1 = New cv.Mat(src.Size, cv.MatType.CV_8U, 0)
        Dim binWidth = CInt(dst1.Width / histogram.Rows)
        histogram.MinMaxLoc(minVal, maxVal)
        Dim splitIndex As Integer
        If maxVal > 0 Then
            For i = 0 To histogram.Rows - 1
                Dim depth = i * depthIncr + 1
                If splitIndex >= ranges.Count - 1 Then splitIndex = ranges.Count - 1

                If depth >= ranges(splitIndex).Y Then splitIndex += 1
                Dim h = CInt(dst1.Height * kalman.kOutput(i) / maxVal)

                If h > 0 Then dst1.Rectangle(New cv.Rect(i * binWidth, dst1.Height - h, binWidth, h), splitIndex + 1, -1)
                barHeight.Add(h)
            Next
        End If

        Dim spread = 255 / ranges.Count
        task.palette.Run(dst1 * spread)
        dst1 = task.palette.dst1
    End Sub
End Class







Public Class Proximity_Clusters : Inherits VBparent
    Public valleys As New Proximity_Basics
    Public Sub New()
        task.desc = "Color each of the Depth Clusters found with Proximity_Basics - stabilized with Kalman."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f.Clone

        valleys.Run(src)
        dst1 = valleys.dst1

        Dim tmp As New cv.Mat
        Dim colorIncr = 255 / valleys.ranges.Count
        Dim paletteSrc = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        For i = 0 To valleys.ranges.Count - 1
            Dim startEndDepth = valleys.ranges.ElementAt(i)
            cv.Cv2.InRange(src, startEndDepth.X, startEndDepth.Y, tmp)
            paletteSrc.SetTo(colorIncr * (i + 1), tmp.ConvertScaleAbs())
        Next
        paletteSrc += 1
        task.palette.Run(paletteSrc)
        dst2 = task.palette.dst1
        If standalone Or task.intermediateName = caller Then
            label1 = "Histogram of " + CStr(valleys.ranges.Count) + " Depth Clusters"
            label2 = "Backprojection of " + CStr(valleys.ranges.Count) + " histogram clusters"
        End If
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class







Public Class Proximity_ClustersKalman : Inherits VBparent
    Dim kalman As New Kalman_Basics
    Public valleys As New Proximity_Basics
    Public Sub New()
        task.desc = "Use Kalman to keep the bar chart similar across frames"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f.Clone

        valleys.Run(src)

        If kalman.kInput.Length - 1 <> valleys.hist.plotHist.hist.Rows Then ReDim kalman.kInput(valleys.hist.plotHist.hist.Rows - 1)
        For i = 0 To valleys.hist.plotHist.hist.Rows - 1
            kalman.kInput(i) = valleys.barValues(i)
        Next
        kalman.Run(src)

        Dim tmp As New cv.Mat
        Dim colorIncr = 255 / valleys.ranges.Count
        dst1 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        dst2 = New cv.Mat(dst2.Size, cv.MatType.CV_8U, 0)
        Dim depthIncr = task.maxDepth / task.histogramBins ' each bar represents this number of millimeters
        Dim binWidth = CInt(dst1.Width / task.histogramBins)
        For i = 0 To kalman.kOutput.Length - 1
            Dim h = valleys.barHeight(i)
            If h > 0 Then dst1.Rectangle(New cv.Rect(i * binWidth, dst1.Height - h, binWidth, h), colorIncr * CInt(kalman.kOutput(i)), -1)

            Dim startDepth = i * depthIncr
            Dim endDepth = (i + 1) * depthIncr
            cv.Cv2.InRange(src, startDepth, endDepth, tmp)
            dst2.SetTo(colorIncr * CInt(kalman.kOutput(i)), tmp.ConvertScaleAbs())
        Next

        task.palette.Run(dst1)
        dst1 = task.palette.dst1
        task.palette.Run(dst2)
        dst2 = task.palette.dst1
        dst2.SetTo(0, task.noDepthMask)
    End Sub
End Class










Public Class Proximity_Valleys : Inherits VBparent
    Dim trends As New SLR_Trends
    Public kalman As New Kalman_Basics
    Public depthRegions As New List(Of Integer)
    Public plotHist As New Plot_Histogram
    Public Sub New()
        task.desc = "Identify ranges by marking histogram entries from valley to valley"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        trends.hist.plotHist.maxRange = task.maxZ * 1000
        trends.hist.depthNoZero = True ' not interested in the undefined depth areas...
        trends.Run(task.depth32f)

        If kalman.kInput.Length <> task.histogramBins Then ReDim kalman.kInput(task.histogramBins - 1)
        kalman.kInput = trends.resultingValues.ToArray
        kalman.Run(src)

        dst1.SetTo(cv.Scalar.Black)
        Dim barWidth = Int(dst1.Width / trends.resultingValues.Count)
        Dim colorIndex As Integer
        Dim color = task.scalarColors(colorIndex Mod 255)
        Dim vals() = {-1, -1, -1}
        For i = 0 To kalman.kOutput.Count - 1
            Dim h = dst1.Height - kalman.kOutput(i)
            vals(0) = vals(1)
            vals(1) = vals(2)
            vals(2) = h
            If vals(0) >= 0 Then
                If vals(0) > vals(1) And vals(2) > vals(1) Then
                    colorIndex += 1
                    color = task.scalarColors(colorIndex Mod 255)
                End If
            End If
            cv.Cv2.Rectangle(dst1, New cv.Rect(i * barWidth, dst1.Height - h, barWidth, h), color, -1)
            depthRegions.Add(colorIndex)
        Next
        label1 = "Depth regions between 0 and " + CStr(CInt(task.maxZ)) + " meters"
    End Sub
End Class









Public Class Proximity_SLR : Inherits VBparent
    Public slr As New SLR_Basics
    Public hist As New Histogram_Basics
    Public Sub New()
        label1 = "Original data"
        task.desc = "Run Segmented Linear Regression on depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32FC1 Then src = task.depth32f
        hist.plotHist.maxRange = task.maxZ * 1000
        hist.depthNoZero = True ' not interested in the undefined depth areas...
        hist.Run(src)
        hist.histogram.Set(Of Single)(0, 0, 0)
        dst1 = hist.dst1
        For i = 0 To hist.histogram.Rows - 1
            slr.input.dataX.Add(i)
            slr.input.dataY.Add(hist.histogram.Get(Of Single)(i, 0))
        Next
        slr.Run(src)
        dst2 = slr.dst2
    End Sub
End Class










Public Class Proximity_KMeans : Inherits VBparent
    Public resizeRequest As Boolean = True
    Public useDepthColor As Boolean = True
    Public Sub New()
        If sliders.Setup(caller) Then sliders.setupTrackBar(0, "kMeans k", 2, 32, 4)
        task.desc = "Cluster the depth image pixels using kMeans."
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        Static kSlider = findSlider("kMeans k")
        Dim kMeansK = kSlider.value

        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f
        Dim labels = New cv.Mat()
        Dim colors As New cv.Mat
        Dim columnVector As New cv.Mat
        If standalone Then src.SetTo(100000, task.noDepthMask)
        columnVector = src.Reshape(1, src.Height * src.Width)

        cv.Cv2.Kmeans(columnVector, kMeansK, labels, term, 1, cv.KMeansFlags.PpCenters, colors)
        labels.Reshape(1, src.Height).ConvertTo(labels, cv.MatType.CV_8U)

        Dim depthVals As New SortedList(Of Single, Integer)
        For i = 0 To kMeansK - 1
            depthVals.Add(colors.Get(Of Single)(i, 0), i)
        Next

        Dim incr = 255 / kMeansK
        Dim myLut As New cv.Mat(1, 256, cv.MatType.CV_8UC1, 0)
        For i = 0 To kMeansK - 1
            myLut.Set(Of Byte)(0, depthVals.ElementAt(i).Value, (i + 1) * incr)
        Next

        dst1 = labels.LUT(myLut)
        If standalone Then dst1.SetTo(0, task.noDepthMask)
    End Sub
End Class








Public Class Proximity_Reduction : Inherits VBparent
    Dim reduction As New Reduction_Basics
    Public counts As New List(Of Integer)
    Public Sub New()
        reduction.radio.check(0).Checked = True
        findSlider("Reduction factor").Value = 800
        label2 = "Reduced depth data before normalizing (32-bit)"
        task.desc = "Use reduction to cluster depth data"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        task.depth32f.ConvertTo(src, cv.MatType.CV_32S)
        reduction.Run(src)
        reduction.dst1.ConvertTo(dst2, cv.MatType.CV_32F)

        dst1 = dst2.Normalize(0, 255, cv.NormTypes.MinMax)
        dst1.ConvertTo(dst1, cv.MatType.CV_8UC1)

        If task.frameCount Mod 30 = 0 Then
            counts.Clear()
            For y = 0 To dst1.Rows - 1 Step dst1.Rows / 10
                For x = 0 To dst1.Cols - 1 Step dst1.Cols / 10
                    Dim val = CInt(dst1.Get(Of Byte)(y, x))
                    If counts.Contains(val) = False Then counts.Add(val)
                Next
            Next
        End If

        task.palette.Run(dst1)
        dst1 = task.palette.dst1
        label1 = reduction.label1 + " with " + CStr(counts.Count) + " levels"
    End Sub
End Class








Public Class Proximity_KMeansFast : Inherits VBparent
    Dim proxy As New Proximity_KMeans
    Public Sub New()
        task.desc = "Use resize with Nearest flag to speed up Proximity_KMeans and preserve accuracy"
    End Sub
    Public Sub Run(src As cv.Mat) ' Rank = 1
        If src.Type <> cv.MatType.CV_32F Then src = task.depth32f
        If standalone Then src.SetTo(100000, task.noDepthMask)
        Dim tmp = src.Resize(New cv.Size(src.Width / 2, src.Height / 2), 0, 0, cv.InterpolationFlags.Nearest)
        proxy.Run(tmp)
        dst1 = proxy.dst1.Resize(src.Size)
        dst1.SetTo(0, task.noDepthMask)
    End Sub
End Class