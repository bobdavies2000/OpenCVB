Imports cv = OpenCvSharp
Namespace VBClasses
    Public Class Correlation_Basics : Inherits TaskParent
        Public cList As New List(Of Single)
        Public corrThreshold As Single
        Public mmRanges As New List(Of Double)
        Dim plotHist As New Plot_Histogram
        Public Sub New()
            plotHist.createHistogram = True
            plotHist.shadeValues = False
            plotHist.minRange = -1
            plotHist.maxRange = 1
            task.gOptions.HistBinBar.Value = task.gOptions.HistBinBar.Maximum
            If standalone Then task.gOptions.displayDst1.Checked = True
            labels(1) = "Click on a rectangle to see the correlation of the current to last image."
            desc = "Measure the correlation of all grid squares."
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            'If task.heartBeat = False Then
            '    SetTrueText(strOut, 1)
            '    Exit Sub
            'End If

            Static lastsrc As cv.Mat = task.gray.Clone
            dst2 = task.gray.Clone
            Dim correlationMat As New cv.Mat
            cList.Clear()
            dst3 = src
            Dim mmList As New List(Of mmData)
            mmRanges.Clear()
            For i = 0 To task.gSquares.Count - 1
                Dim r = task.gSquares(i)
                cv.Cv2.MatchTemplate(task.gray(r), lastsrc(r), correlationMat, cv.TemplateMatchModes.CCoeffNormed)

                Dim corr = correlationMat.Get(Of Single)(0, 0) + 1
                cList.Add(corr)
                Dim mm = GetMinMax(task.gray(r))
                mmList.Add(mm)
                mmRanges.Add(mm.range)
            Next

            lastsrc = task.gray.Clone

            If cList.Count > 0 Then
                Dim inputAdjusted = cv.Mat.FromPixelData(cList.Count, 1, cv.MatType.CV_32F, cList.ToArray) - 1
                plotHist.Run(inputAdjusted)
                dst3 = plotHist.dst2

                Dim lastEntry = plotHist.histArray.Last
                labels(2) = "Correlation Min = " + Format(cList.Min - 1, fmt1) + ", Max = " + Format(cList.Max - 1, fmt1)
                labels(3) = CStr(lastEntry) + " (" + Format(lastEntry / task.gSquares.Count, "0%") +
                            ") had correlation >= " + Format(corrThreshold - 1, fmt2) + "  Plot below ranges from -1 to 1"

                corrThreshold = 2.0 - 2.0 / task.histogramBins
                Dim mmRangeTest As New List(Of Double)
                For i = 0 To task.gSquares.Count - 1
                    Dim r = task.gSquares(i)
                    If cList(i) < corrThreshold Then
                        dst2.Rectangle(r, white, task.lineWidth)
                        mmRangeTest.Add(mmRanges(i))
                    End If
                Next

                Dim index = task.gridMap.Get(Of Integer)(task.clickPoint.Y, task.clickPoint.X)
                Dim mm = mmList(index)
                strOut = "Click on any grid rect to see its grayscale range." + vbCrLf +
                         "Min gray = " + Format(mm.minVal, fmt0) + vbCrLf +
                         "Max Gray = " + Format(mm.maxVal, fmt0) + vbCrLf +
                         "Range = " + Format(mm.range, fmt0) + vbCrLf + vbCrLf +
                         "Surveying the deteriorated correlations:" + vbCrLf +
                         If(mmRangeTest.Count = 0, "",
                         "Min Range = " + Format(mmRangeTest.Min, fmt1) + vbCrLf +
                         "Max Range = " + Format(mmRangeTest.Max, fmt1))
                SetTrueText(strOut, 1)
            End If
        End Sub
    End Class






    Public Class NR_Correlation_Basics : Inherits TaskParent
        Dim kFlood As New KMeans_Edges
        Dim options As New Options_FeatureMatch
        Public Sub New()
            labels(3) = "Plot of z (vertical scale) to x with ranges shown on the plot."
            desc = "Compute a correlation for src rows (See also: Match.vb"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            options.Run()

            kFlood.Run(src)
            dst1 = kFlood.dst2
            dst2 = kFlood.dst3

            Dim row = task.mouseMovePoint.Y
            If row = 0 Then SetTrueText("Move mouse across image to see the relationship between X and Z" + vbCrLf +
                                        "A linear relationship is a useful correlation", New cv.Point(0, 10), 3)

            Dim dataX As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, cv.Scalar.All(0))
            Dim dataY As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, cv.Scalar.All(0))
            Dim dataZ As New cv.Mat(New cv.Size(src.Width, src.Height), cv.MatType.CV_32F, cv.Scalar.All(0))

            Dim mask = kFlood.dst3.CvtColor(cv.ColorConversionCodes.BGR2GRAY)
            task.pcSplit(0).CopyTo(dataX, mask)
            task.pcSplit(1).CopyTo(dataY, mask)
            task.pcSplit(2).CopyTo(dataZ, mask)

            Dim row1 = dataX.Row(row)
            Dim row2 = dataZ.Row(row)
            dst2.Line(New cv.Point(0, row), New cv.Point(dst2.Width, row), cv.Scalar.Yellow, task.lineWidth + 1)

            Dim correlationmat As New cv.Mat
            cv.Cv2.MatchTemplate(row1, row2, correlationmat, options.matchOption)
            Dim correlation = correlationmat.Get(Of Single)(0, 0)
            labels(2) = "Correlation of X to Z = " + Format(correlation, fmt2)

            dst3.SetTo(0)
            Dim plotX As New List(Of Single)
            Dim plotZ As New List(Of Single)
            For i = 0 To row1.Cols - 1
                Dim x = row1.Get(Of Single)(0, i)
                Dim z = row2.Get(Of Single)(0, i)
                If x <> 0 And z <> 0 Then
                    plotX.Add(x)
                    plotZ.Add(z)
                End If
            Next

            If plotX.Count > 0 Then
                Dim minx = plotX.Min, maxx = plotX.Max
                Dim minZ = plotZ.Min, maxZ = plotZ.Max
                For i = 0 To plotX.Count - 1
                    Dim x = dst3.Width * (plotX(i) - minx) / (maxx - minx)
                    Dim y = dst3.Height * (plotZ(i) - minZ) / (maxZ - minZ)
                    DrawCircle(dst3, New cv.Point(x, y), task.DotSize, cv.Scalar.Yellow)
                Next
                SetTrueText("Z-min " + Format(minZ, fmt2), New cv.Point(10, 5), 3)
                SetTrueText("Z-max " + Format(maxZ, fmt2) + vbCrLf + vbTab + "X-min " + Format(minx, fmt2), New cv.Point(0, dst3.Height - 20), 3)
                SetTrueText("X-max " + Format(maxx, fmt2), New cv.Point(dst3.Width - 40, dst3.Height - 10), 3)
            End If
        End Sub
    End Class




    Public Class Correlation_Interactive : Inherits TaskParent
        Dim plot As New Plot_Interactive
        Public Sub New()
            desc = "Plot the range of correlations and display their source - duplicate of Plot_Interactive"
        End Sub
        Public Overrides Sub RunAlg(src As cv.Mat)
            plot.Run(src)
            dst2 = plot.dst2
            dst3 = plot.dst3
            labels(2) = plot.labels(2)
            labels(3) = plot.labels(3)
        End Sub
    End Class

End Namespace