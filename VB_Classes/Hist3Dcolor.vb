﻿Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Hist3Dcolor_Basics : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Core
    Public classCount As Integer
    Public Sub New()
        desc = "Build a 3D histogram from the BGR image on each heartbeat.  Backproject each frame."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 3 Then src = task.color

        If heartBeat() Then hist3d.Run(src)
        classCount = hist3d.classCount + 1

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, hist3d.histogram, dst2, redOptions.rangesBGR)
        dst2 += 1
        dst3 = vbPalette(dst2 * 255 / classCount)

        Dim sum = hist3d.histogramList.Sum
        labels(2) = CStr(sum) + " pixels (" + Format(sum / src.Total, "0%") + ") classified into bins 1-" +
                    CStr(classCount)
        labels(3) = "Backprojection of the top " + CStr(classCount) + " histogram entries."
    End Sub
End Class








Public Class Hist3Dcolor_UniqueRGBPixels : Inherits VB_Algorithm
    Dim hist3D As New Hist3Dcolor_Basics_CPP
    Public pixels As New List(Of cv.Point3f)
    Public counts As New List(Of Integer)
    Public Sub New()
        desc = "Get the number of non-zero BGR elements in the 3D color histogram of the current image and their BGR values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3D.Run(src)

        pixels.Clear()
        counts.Clear()
        Dim bins As Integer = redOptions.HistBinSlider.Value
        For z = 0 To bins - 1
            For y = 0 To bins - 1
                For x = 0 To bins - 1
                    Dim val = CInt(hist3D.histogram.Get(Of Single)(x * bins * bins + y * bins + z, 0))
                    If val > 0 Then
                        pixels.Add(New cv.Point3f(CInt(255 * x / bins), CInt(255 * y / bins), CInt(255 * z / bins)))
                        counts.Add(val)
                    End If
                Next
            Next
        Next
        setTrueText("There are " + CStr(pixels.Count) + " non-zero entries in the 3D histogram " + vbCrLf + "See uniquePixels list in Hist3Dcolor_UniquePixels", 2)
    End Sub
End Class








Public Class Hist3Dcolor_TopXColors : Inherits VB_Algorithm
    Dim unique As New Hist3Dcolor_UniqueRGBPixels
    Public topXPixels As New List(Of cv.Point3i)
    Public mapTopX As Integer = 16
    Public Sub New()
        desc = "Get the top 256 of non-zero BGR elements in the 3D color histogram of the current image and their BGR values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        unique.Run(src)

        Dim sortedPixels As New SortedList(Of Integer, cv.Point3f)(New compareAllowIdenticalIntegerInverted)
        For i = 0 To unique.pixels.Count - 1
            sortedPixels.Add(unique.counts(i), unique.pixels(i))
        Next

        topXPixels.Clear()

        For i = 0 To sortedPixels.Count - 1
            topXPixels.Add(sortedPixels.ElementAt(i).Value.ToPoint3i)
            If topXPixels.Count >= mapTopX Then Exit For
        Next
        setTrueText("There are " + CStr(sortedPixels.Count) + " non-zero entries in the 3D histogram " + vbCrLf + "The top " + CStr(mapTopX) + " pixels are in topXPixels", 2)
    End Sub
End Class








Public Class Hist3Dcolor_Distribution : Inherits VB_Algorithm
    Dim plot As New Plot_Histogram
    Dim hist As New Histogram_Basics
    Dim histogram As New cv.Mat
    Public Sub New()
        desc = "Build a 3D histogram from the BGR image, backproject it, and plot the histogram of the backprojection."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = redOptions.HistBinSlider.Value
        Dim hBins() As Integer = {bins, bins, bins}
        cv.Cv2.CalcHist({src}, {0, 1, 2}, New cv.Mat, histogram, 3, hBins, redOptions.rangesBGR)

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, redOptions.rangesBGR)
        If heartBeat() Then
            hist.Run(dst2)
            plot.Run(hist.histogram)
            dst3 = plot.dst2
        End If
    End Sub
End Class








Public Class Hist3Dcolor_BP_Filter : Inherits VB_Algorithm
    Dim plot As New Plot_Histogram
    Dim hist As New Histogram_Basics
    Dim hist3d As New Hist3Dcolor_Core
    Public Sub New()
        desc = "Build a 3D histogram from the BGR image, backproject it, and plot the histogram of the backprojection."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)

        For i = 0 To hist3d.histList.Count - 1
            If hist3d.histList(i) < hist3d.options.threshold3D Then hist3d.histList(i) = 0
        Next

        Marshal.Copy(hist3d.histList, 0, hist3d.histogram.Data, hist3d.histList.Length)

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, hist3d.histogram, dst2, redOptions.rangesBGR)
        If heartBeat() Then
            hist.Run(dst2)

            If gOptions.DebugCheckBox.Checked = False Then
                Dim test(hist.histogram.Total - 1) As Single
                Marshal.Copy(hist.histogram.Data, test, 0, test.Length)
                test(task.histogramBins - 1) = 0

                Marshal.Copy(test, 0, hist.histogram.Data, test.Length)
            End If
            plot.Run(hist.histogram)
            dst3 = plot.dst2
        End If
    End Sub
End Class










Public Class Hist3Dcolor_SortedSelect : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Core
    Public Sub New()
        labels(3) = "The highlighted pixels are in the selected bin"
        desc = "Build a 3D histogram from the BGR image and backproject the 'Selected bin' (in options_HistXD sliders)."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static saveCount As Integer, saveBin As Integer
        If heartBeat() Then
            hist3d.Run(src)
            Dim selectedBin = hist3d.options.selectedBin
            saveBin = hist3d.sortedHist.ElementAt(selectedBin).Value
            saveCount = hist3d.sortedHist.ElementAt(selectedBin).Key

            ReDim hist3d.histList(hist3d.histList.Count - 1)
            hist3d.histList(saveBin) = 255

            Marshal.Copy(hist3d.histList, 0, hist3d.histogram.Data, hist3d.histList.Length)
        End If

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, hist3d.histogram, dst2, redOptions.rangesBGR)
        dst3 = src.Clone
        dst3.SetTo(cv.Scalar.White, dst2)

        labels(2) = CStr(saveCount) + " pixels were found in bin " + CStr(saveBin)
    End Sub
End Class







Public Class Hist3Dcolor_Reduction : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Basics
    Dim reduction As New Reduction_BGR
    Public classCount As Integer
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.SimpleReductionSlider.Value = 45
        desc = "Backproject the 3D histogram for RGB after reduction"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 3 Then src = task.color
        reduction.Run(src)

        hist3d.Run(reduction.dst2)
        dst1 = reduction.dst2
        dst2 = hist3d.dst2
        dst3 = hist3d.dst3
        labels(2) = hist3d.labels(2)
    End Sub
End Class









Public Class Hist3Dcolor_Basics_CPP : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public prepareImage As Boolean = True
    Public Sub New()
        labels(2) = "dst2 = backprojection (8UC1 format). The 3D histogram is in histogram."
        desc = "Build a 3D histogram from the BGR image and sort it by histogram entry size."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim bins = redOptions.HistBinSlider.Value
        Dim imagePtr = Hist3Dcolor_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins)
        handleInput.Free()

        histogram = New cv.Mat(bins * bins * bins, 1, cv.MatType.CV_32F, imagePtr)
        If prepareImage Then
            Dim samples(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, samples, 0, samples.Length)

            Dim sortedHist As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)

            For i = 0 To samples.Count - 1
                sortedHist.Add(samples(i), i)
            Next
            For i = 0 To sortedHist.Count - 1
                Dim key = sortedHist.ElementAt(i)
                Dim index = key.Value
                samples(index) = i + 1
            Next

            Marshal.Copy(samples, 0, histogram.Data, samples.Length)

            cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, redOptions.rangesBGR)

            Dim mm = vbMinMax(dst2)

            dst3 = vbPalette(dst2 * 255 / mm.maxVal)
            labels(3) = CStr(mm.maxVal) + " different levels in the backprojection."
        End If
    End Sub
End Class








Public Class Hist3Dcolor_ZeroGroups : Inherits VB_Algorithm
    Public maskInput As New cv.Mat
    Public classCount As Integer
    Public histogram As New cv.Mat
    Public Sub New()
        desc = "Breakdown the 3D histogram using the '0' entries as boundaries between clusters."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 3 Then src = task.color

        If task.optionsChanged Then
            Dim bins = redOptions.HistBinSlider.Value
            Dim hBins() As Integer = {bins, bins, bins}
            cv.Cv2.CalcHist({src}, {0, 1, 2}, maskInput, histogram, 3, hBins, redOptions.rangesBGR)

            Dim histList(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histList, 0, histList.Length)

            Dim boundaries As New List(Of Integer)
            Dim zeroMode As Boolean
            For i = 0 To histList.Count - 1
                If histList(i) = 0 And zeroMode = False Then
                    boundaries.Add(i)
                    zeroMode = True
                End If
                If histList(i) <> 0 Then zeroMode = False
            Next

            Dim lastIndex As Integer
            classCount = 1
            For Each index In boundaries
                For i = lastIndex To index
                    histList(i) = classCount
                Next
                lastIndex = index + 1
                classCount += 1
            Next

            For i = lastIndex To histList.Count - 1
                histList(i) = classCount
            Next
            classCount += 1

            Marshal.Copy(histList, 0, histogram.Data, histList.Length)
        End If
        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, redOptions.rangesBGR)
        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(2) = "Hist3Dcolor_ZeroGroups classCount = " + CStr(classCount)
    End Sub
End Class







Public Class Hist3Dcolor_Dominant : Inherits VB_Algorithm
    Dim rMin As New RedMin_Basics
    Dim hist3d As New Hist3Dcolor_Core
    Public Sub New()
        hist3d.sortHistogramData = False
        desc = "Find the dominant color in a 3D color histogram and backProject it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(2)

        Dim bins = redOptions.HistBinSlider.Value
        Dim guidedHist(bins * bins * bins - 1) As Single
        Dim guidedCounts(bins * bins * bins - 1) As Single
        For Each rp In rMin.minCells
            hist3d.maskInput = rp.mask
            hist3d.Run(src(rp.rect))
            Dim maxVal = hist3d.histogramList.Max
            Dim index = hist3d.histogramList.IndexOf(maxVal)
            guidedHist(index) += rp.index
            guidedCounts(index) += maxVal
        Next

        Dim histogram = hist3d.histogram.Clone
        If histogram.Rows <> 0 Then
            guidedHist(0) = 0
            Marshal.Copy(guidedHist, 0, histogram.Data, guidedHist.Length)
            Dim classCount = histogram.CountNonZero
            cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst1, redOptions.rangesBGR)
            dst3 = vbPalette(dst1 * 255 / classCount)
            If heartBeat() Then
                labels(3) = "Back projection of the dominant color entries.  ClassCount = " + CStr(classCount) +
                            " or " + Format(guidedCounts.Sum / src.Total, "0%") + " of the image."
            End If
        End If
    End Sub
End Class








Public Class Hist3Dcolor_Core : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public histogramList As New List(Of Single)
    Public histList() As Single
    Public classCount As Integer
    Public options As New Options_HistXD
    Public sortedHist As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)
    Public sortHistogramData As Boolean = True
    Public maskInput As New cv.Mat
    Public Sub New()
        desc = "Build a 3D histogram from the BGR image and sorted it by histogram entry size."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()
        If src.Channels <> 3 Then src = task.color

        Dim bins = redOptions.HistBinSlider.Value
        Dim hBins() As Integer = {bins, bins, bins}
        cv.Cv2.CalcHist({src}, {0, 1, 2}, maskInput, histogram, 3, hBins, redOptions.rangesBGR)

        ReDim histList(histogram.Total - 1)
        Marshal.Copy(histogram.Data, histList, 0, histList.Length)
        histogramList = histList.ToList

        If sortHistogramData Then
            sortedHist.Clear()
            For i = 0 To histList.Count - 1
                sortedHist.Add(histList(i), i)
            Next

            classCount = 0
            Dim sum = 0
            For Each el In sortedHist
                histList(el.Value) = classCount
                classCount += 1
                sum += el.Key
                If sum > src.Total * redOptions.imageThresholdPercent Or
                    classCount = redOptions.DesiredCellSlider.Value Then Exit For
            Next

            For i = classCount To histList.Count - 1
                Dim index = sortedHist.ElementAt(i).Value
                histList(index) = classCount
            Next
            Marshal.Copy(histList, 0, histogram.Data, histList.Length)
        End If
        If standalone Then setTrueText("There is no output when standalone.  Use Hist3Dcolor_Basics to test.")
    End Sub
End Class






Public Class Hist3Dcolor_PlotHist1D : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Core
    Dim plot As New Plot_Histogram
    Public histogram As cv.Mat
    Public histArray() As Single
    Public Sub New()
        hist3d.sortHistogramData = False
        plot.removeZeroEntry = False
        labels(2) = "The 3D histogram of the RGB image stream - note the number of gaps"
        desc = "Present the 3D histogram as a typical histogram bar chart."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)
        ReDim histArray(hist3d.histogram.Total - 1)
        Marshal.Copy(hist3d.histogram.Data, histArray, 0, histArray.Length)

        histogram = New cv.Mat(histArray.Count, 1, cv.MatType.CV_32F, histArray)
        plot.Run(histogram)
        dst2 = plot.dst2
    End Sub
End Class






Public Class Hist3Dcolor_Diff : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Basics
    Dim diff As New Diff_Basics
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 0
        desc = "Create a mask for the pixels that are changing with every frame. Can the color cells be merged?"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)
        dst2 = hist3d.dst3
        labels(2) = hist3d.labels(3)

        diff.Run(hist3d.dst2)
        dst3 = diff.dst3
    End Sub
End Class








Public Class Hist3Dcolor_BuildHistogram : Inherits VB_Algorithm
    Public threshold As Integer
    Public classCount As Integer
    Public Sub New()
        desc = "Build a simulated 3D histogram from the read 3D histogram."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then
            Static plot2D As New Hist3Dcloud_PlotHist1D
            plot2D.Run(src)
            src = plot2D.histogram
        End If

        Dim histArray(src.Total - 1) As Single
        Marshal.Copy(src.Data, histArray, 0, histArray.Length)

        classCount = 1
        Dim index As Integer
        For i = index To histArray.Count - 1
            For index = index To histArray.Count - 1
                If histArray(index) > threshold Then Exit For
                histArray(index) = classCount
            Next
            classCount += 1
            For index = index To histArray.Count - 1
                If histArray(index) <= threshold Then Exit For
                histArray(index) = classCount
            Next
            If index >= histArray.Count Then Exit For
        Next

        Marshal.Copy(histArray, 0, src.Data, histArray.Length)
        dst2 = src.Clone
    End Sub
End Class