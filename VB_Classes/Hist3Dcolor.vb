Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Hist3Dcolor_Basics : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public histogram1D As New cv.Mat
    Public classCount As Integer
    Public options As New Options_HistXD
    Public maskInput As New cv.Mat
    Public histArray() As Single
    Public simK As New Hist3Dcolor_BuildHistogram
    Public alwaysRun As Boolean
    Public Sub New()
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf
        desc = "Capture a 3D color histogram, find the gaps, and backproject the clusters found."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        If heartBeat() Or alwaysRun Then
            If src.Channels <> 3 Then src = task.color
            Dim bins = redOptions.HistBinSlider.Value
            cv.Cv2.CalcHist({src}, {0, 1, 2}, maskInput, histogram, 3, {bins, bins, bins}, redOptions.rangesBGR)

            ReDim histArray(histogram.Total - 1)
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
            histogram1D = New cv.Mat(histArray.Count, 1, cv.MatType.CV_32F, histArray)

            simK.Run(histogram)
            histogram = simK.dst2
            classCount = simK.classCount
        End If

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, redOptions.rangesBGR)
        dst3 = vbPalette(dst2 * 255 / classCount)

        labels(2) = simK.labels(2)
        labels(3) = "Backprojection of " + CStr(classCount) + " histogram entries."
    End Sub
End Class







Public Class Hist3Dcolor_UniqueRGBPixels : Inherits VB_Algorithm
    Dim hist3D As New Hist3Dcolor_Basics
    Public pixels As New List(Of cv.Point3f)
    Public counts As New List(Of Integer)
    Public Sub New()
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf
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
                    Dim val = hist3D.histArray(x * bins * bins + y * bins + z)
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
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf
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









Public Class Hist3Dcolor_Reduction : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Basics
    Dim reduction As New Reduction_BGR
    Public classCount As Integer
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.SimpleReductionSlider.Value = 45
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf + "Secondary: redOptions 'Simple Reduction'"
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







Public Class Hist3Dcolor_ZeroGroups : Inherits VB_Algorithm
    Public maskInput As New cv.Mat
    Public classCount As Integer
    Public histogram As New cv.Mat
    Public Sub New()
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf
        desc = "Breakdown the 3D histogram using the '0' entries as boundaries between clusters."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Channels <> 3 Then src = task.color

        If task.optionsChanged Then
            Dim bins = redOptions.HistBinSlider.Value
            Dim hBins() As Integer = {bins, bins, bins}
            cv.Cv2.CalcHist({src}, {0, 1, 2}, maskInput, histogram, 3, hBins, redOptions.rangesBGR)

            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)

            Dim boundaries As New List(Of Integer)
            Dim zeroMode As Boolean
            For i = 0 To histArray.Count - 1
                If histArray(i) = 0 And zeroMode = False Then
                    boundaries.Add(i)
                    zeroMode = True
                End If
                If histArray(i) <> 0 Then zeroMode = False
            Next

            Dim lastIndex As Integer
            classCount = 1
            For Each index In boundaries
                For i = lastIndex To index
                    histArray(i) = classCount
                Next
                lastIndex = index + 1
                classCount += 1
            Next

            For i = lastIndex To histArray.Count - 1
                histArray(i) = classCount
            Next
            classCount += 1

            Marshal.Copy(histArray, 0, histogram.Data, histArray.Length)
        End If
        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, redOptions.rangesBGR)
        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(2) = "Hist3Dcolor_ZeroGroups classCount = " + CStr(classCount)
    End Sub
End Class







Public Class Hist3Dcolor_PlotHist1D : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Basics
    Dim plot As New Plot_Histogram
    Public histogram1D As cv.Mat
    Public histogram As cv.Mat
    Public histArray() As Single
    Public Sub New()
        hist3d.alwaysRun = True
        plot.removeZeroEntry = False
        labels(2) = "The 3D histogram of the RGB image stream - note the number of gaps"
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf
        desc = "Present the 3D histogram as a typical histogram bar chart."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)
        histogram1D = hist3d.histogram1D
        histArray = hist3d.histArray
        plot.Run(hist3d.histogram1D)
        dst2 = plot.dst2
    End Sub
End Class







Public Class Hist3Dcolor_Select : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Basics
    Public Sub New()
        labels(3) = "The highlighted pixels are in the selected bin"
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf + "Secondary: goptions 'DebugSlider'"
        desc = "Build a 3D histogram from the BGR image and backproject the 'Selected bin' (in options_HistXD sliders)."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)

        Dim selection = gOptions.DebugSlider.Value
        dst2 = hist3d.dst2.InRange(selection, selection)
        Dim saveCount = dst2.CountNonZero

        dst3 = src.Clone
        dst3.SetTo(cv.Scalar.White, dst2)

        labels(2) = CStr(saveCount) + " pixels were found in bin " + CStr(selection)
    End Sub
End Class










Public Class Hist3Dcolor_Basics_CPP : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public prepareImage As Boolean = True
    Public histogram1D As New cv.Mat
    Public simK As New Hist3Dcolor_BuildHistogram
    Public classCount As Integer
    Public Sub New()
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf
        desc = "Build a 3D histogram from the BGR image and sort it by histogram entry size."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim bins = redOptions.HistBinSlider.Value
        Dim imagePtr = Hist3Dcolor_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins)
        handleInput.Free()

        histogram = New cv.Mat(redOptions.bins3D, 1, cv.MatType.CV_32F, imagePtr)
        If prepareImage Then
            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
            histogram1D = New cv.Mat(histArray.Count, 1, cv.MatType.CV_32F, histArray)

            simK.Run(histogram)
            histogram = simK.dst2
            classCount = simK.classCount

            cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, redOptions.rangesBGR)

            Dim mm = vbMinMax(dst2)

            dst3 = vbPalette(dst2 * 255 / mm.maxVal)
            labels(2) = simK.labels(2)
            labels(3) = CStr(mm.maxVal) + " different levels in the backprojection."
        End If
    End Sub
End Class







Public Class Hist3Dcolor_Diff : Inherits VB_Algorithm
    Dim hist3d As New Hist3Dcolor_Basics
    Dim diff As New Diff_Basics
    Public Sub New()
        gOptions.PixelDiffThreshold.Value = 0
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf + "Secondary: goptions 'Pixel Difference'"
        desc = "Create a mask for the color pixels that are changing with every frame of the Hist3Dcolor_basics."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)
        dst2 = hist3d.dst3
        labels(2) = hist3d.labels(3)

        diff.Run(hist3d.dst2)
        dst3 = diff.dst3
    End Sub
End Class







Public Class Hist3Dcolor_Dominant : Inherits VB_Algorithm
    Dim rMin As New RedMin_Basics
    Dim hist3d As New Hist3Dcolor_Basics
    Public Sub New()
        dst1 = New cv.Mat(dst1.Size, cv.MatType.CV_8U, 0)
        labels(3) = "Dominant colors in each cell backprojected with the each cell's index."
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf
        desc = "Find the dominant color in a 3D color histogram and backProject it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        rMin.Run(src)
        dst2 = rMin.dst3
        labels(2) = rMin.labels(2)

        Dim bins3D =
        dst1.SetTo(0)
        For Each rp In rMin.minCells
            hist3d.maskInput = rp.mask
            hist3d.Run(src(rp.rect))
            Dim index = hist3d.histArray.ToList.IndexOf(hist3d.histArray.Max)

            Dim guidedHist(redOptions.bins3D - 1) As Single
            guidedHist(index) = rp.index

            Marshal.Copy(guidedHist, 0, hist3d.histogram.Data, guidedHist.Length)
            cv.Cv2.CalcBackProject({src(rp.rect)}, {0, 1, 2}, hist3d.histogram, dst1(rp.rect), redOptions.rangesBGR)
        Next
        dst3 = vbPalette(dst1 * 255 / rMin.minCells.Count)
    End Sub
End Class







Public Class Hist3Dcolor_BuildHistogram : Inherits VB_Algorithm
    Public threshold As Integer
    Public classCount As Integer
    Public histArray() As Single
    Public Sub New()
        advice = "Primary: redOptions '3D Histogram Bins' slider" + vbCrLf
        desc = "Build a simulated (guided) 3D histogram from the 3D histogram supplied in src."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC1 Then
            Static plot1D As New Hist3Dcloud_PlotHist1D
            plot1D.Run(src)
            src = plot1D.histogram
        End If

        ReDim histArray(src.Total - 1)
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

        Dim minClass = histArray.Min - 1
        If minClass <> 0 Then
            src -= minClass
            For i = 0 To histArray.Count - 1
                histArray(i) -= minClass
            Next
            classCount -= minClass
        End If
        dst2 = src.Clone
        Marshal.Copy(histArray, 0, dst2.Data, histArray.Length)
        labels(2) = "Histogram entries vary from " + CStr(histArray.Min) + " to " + CStr(classCount) + " inclusive"
    End Sub
End Class