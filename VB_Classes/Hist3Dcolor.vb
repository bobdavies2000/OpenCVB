Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Hist3Dcolor_Basics : Inherits TaskParent
    Public histogram As New cv.Mat
    Public histogram1D As New cv.Mat
    Public classCount As Integer
    Public inputMask As New cv.Mat
    Public histArray() As Single
    Public simK As New Hist3D_BuildHistogram
    Public alwaysRun As Boolean
    Public noMotionMask As Boolean
    Public Sub New()
        desc = "Capture a 3D color histogram, find the gaps, and backproject the clusters found."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Type <> cv.MatType.CV_8UC3 Then src = task.color
        If task.heartBeat Or alwaysRun Or histogram.Width = 0 Then
            Dim bins = task.redOptions.HistBinBar3D.Value
            cv.Cv2.CalcHist({src}, {0, 1, 2}, inputMask, histogram, 3, {bins, bins, bins}, task.redOptions.rangesBGR)

            ReDim histArray(histogram.Total - 1)
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
            histogram1D = cv.Mat.FromPixelData(histogram.Total, 1, cv.MatType.CV_32F, histArray)

            simK.Run(histogram1D)
            histogram = simK.dst2
            classCount = simK.classCount
        End If

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, task.redOptions.rangesBGR)

        dst3 = ShowPalette(dst2)

        labels(2) = simK.labels(2)
        labels(3) = "Backprojection of " + CStr(classCount) + " histogram entries."
    End Sub
End Class







Public Class Hist3Dcolor_UniqueRGBPixels : Inherits TaskParent
    Dim hColor As New Hist3Dcolor_Basics
    Public pixels As New List(Of cv.Point3f)
    Public counts As New List(Of Integer)
    Public Sub New()
        desc = "Get the number of non-zero BGR elements in the 3D color histogram of the current image and their BGR values"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        hColor.Run(src)

        pixels.Clear()
        counts.Clear()
        Dim bins As Integer = task.redOptions.HistBinBar3D.Value
        For z = 0 To bins - 1
            For y = 0 To bins - 1
                For x = 0 To bins - 1
                    Dim val = hColor.histArray(x * bins * bins + y * bins + z)
                    If val > 0 Then
                        pixels.Add(New cv.Point3f(CInt(255 * x / bins), CInt(255 * y / bins), CInt(255 * z / bins)))
                        counts.Add(val)
                    End If
                Next
            Next
        Next
        SetTrueText("There are " + CStr(pixels.Count) + " non-zero entries in the 3D histogram " + vbCrLf + "See uniquePixels list in Hist3Dcolor_UniquePixels", 2)
    End Sub
End Class








Public Class Hist3Dcolor_TopXColors : Inherits TaskParent
    Dim unique As New Hist3Dcolor_UniqueRGBPixels
    Public topXPixels As New List(Of cv.Point3i)
    Public mapTopX As Integer = 16
    Public Sub New()
        desc = "Get the top 256 of non-zero BGR elements in the 3D color histogram of the current image and their BGR values"
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
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
        SetTrueText("There are " + CStr(sortedPixels.Count) + " non-zero entries in the 3D histogram " + vbCrLf + "The top " + CStr(mapTopX) + " pixels are in topXPixels", 2)
    End Sub
End Class








Public Class Hist3Dcolor_ZeroGroups : Inherits TaskParent
    Public maskInput As New cv.Mat
    Public classCount As Integer
    Public histogram As New cv.Mat
    Public Sub New()
        desc = "Breakdown the 3D histogram using the '0' entries as boundaries between clusters."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Channels() <> 3 Then src = task.color

        If task.optionsChanged Then
            Dim bins = task.redOptions.HistBinBar3D.Value
            Dim hBins() As Integer = {bins, bins, bins}
            cv.Cv2.CalcHist({src}, {0, 1, 2}, maskInput, histogram, 3, hBins, task.redOptions.rangesBGR)

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
        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, task.redOptions.rangesBGR)
        dst3 = ShowPalette(dst2)
        labels(2) = "Hist3Dcolor_ZeroGroups classCount = " + CStr(classCount)
    End Sub
End Class







Public Class Hist3Dcolor_PlotHist1D : Inherits TaskParent
    Dim hColor As New Hist3Dcolor_Basics
    Dim plot As New Plot_Histogram
    Public histogram1D As cv.Mat
    Public histogram As cv.Mat
    Public histArray() As Single
    Public Sub New()
        hColor.alwaysRun = True
        plot.removeZeroEntry = False
        labels(2) = "The 3D histogram of the RGB image stream - note the number of gaps"
        desc = "Present the 3D histogram as a typical histogram bar chart."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        hColor.Run(src)
        histogram1D = hColor.histogram1D
        histArray = hColor.histArray
        plot.Run(hColor.histogram1D)
        dst2 = plot.dst2
    End Sub
End Class







Public Class Hist3Dcolor_Select : Inherits TaskParent
    Dim hColor As New Hist3Dcolor_Basics
    Public Sub New()
        labels(3) = "The highlighted pixels are in the selected bin"
        desc = "Build a 3D histogram from the BGR image and backproject the 'Selected bin' (in options_HistXD sliders)."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        hColor.Run(src)

        Dim selection = task.gOptions.DebugSlider.Value
        dst2 = hColor.dst2.InRange(selection, selection)
        Dim saveCount = dst2.CountNonZero

        dst3 = src.Clone
        dst3.SetTo(white, dst2)

        labels(2) = CStr(saveCount) + " pixels were found in bin " + CStr(selection)
    End Sub
End Class










Public Class Hist3Dcolor_Basics_CPP : Inherits TaskParent
    Public histogram As New cv.Mat
    Public prepareImage As Boolean = True
    Public histogram1D As New cv.Mat
    Public simK As New Hist3D_BuildHistogram
    Public classCount As Integer
    Public Sub New()
        desc = "Build a 3D histogram from the BGR image and sort it by histogram entry size."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim bins = task.redOptions.HistBinBar3D.Value
        Dim imagePtr = Hist3Dcolor_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins)
        handleInput.Free()

        histogram = cv.Mat.FromPixelData(task.redOptions.histBins3D, 1, cv.MatType.CV_32F, imagePtr)
        If prepareImage Then
            Dim histArray(histogram.Total - 1) As Single
            Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
            histogram1D = cv.Mat.FromPixelData(histArray.Count, 1, cv.MatType.CV_32F, histArray)

            simK.Run(histogram)
            histogram = simK.dst2
            classCount = simK.classCount

            cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, task.redOptions.rangesBGR)

            Dim mm As mmData = GetMinMax(dst2)

            dst3 = ShowPalette(dst2)
            labels(2) = simK.labels(2)
            labels(3) = CStr(mm.maxVal) + " different levels in the backprojection."
        End If
    End Sub
End Class







Public Class Hist3Dcolor_Diff : Inherits TaskParent
    Dim hColor As New Hist3Dcolor_Basics
    Dim diff As New Diff_Basics
    Public Sub New()
        task.gOptions.pixelDiffThreshold = 0
        dst3 = New cv.Mat(dst3.Size(), cv.MatType.CV_8U, cv.Scalar.All(0))
        desc = "Create a mask for the color pixels that are changing with every frame of the Hist3Dcolor_basics."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        hColor.Run(src)
        dst2 = hColor.dst3
        labels(2) = hColor.labels(3)

        diff.Run(hColor.dst2)

        If task.heartBeat Then dst3.SetTo(0)
        dst3 = dst3 Or diff.dst2
    End Sub
End Class







Public Class Hist3Dcolor_Vector : Inherits TaskParent
    Public histogram As New cv.Mat
    Public inputMask As New cv.Mat
    Public histArray() As Single
    Public simK As New Hist3D_BuildHistogram
    Dim binArray() As Integer
    Public Sub New()
        Dim bins = task.redOptions.HistBinBar3D.Value
        binArray = {bins, bins, bins}
        desc = "Capture a 3D color histogram for input src - likely to be src(rect)."
    End Sub
    Public Overrides sub RunAlg(src As cv.Mat)
        If src.Channels() <> 3 Then src = task.color
        If task.optionsChanged Then
            Dim bins = task.redOptions.HistBinBar3D.Value
            binArray = {bins, bins, bins}
        End If

        cv.Cv2.CalcHist({src}, {0, 1, 2}, inputMask, histogram, 3, binArray, task.redOptions.rangesBGR)

        ReDim histArray(histogram.Total - 1)
        Marshal.Copy(histogram.Data, histArray, 0, histArray.Length)
        If standaloneTest() Then SetTrueText("Vector prepared in histArray")
    End Sub
End Class