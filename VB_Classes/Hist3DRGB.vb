Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Hist3DRGB_Basics : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 256), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
    Public channels() As Integer = {0, 1, 2}
    Public Sub New()
        gOptions.HistBinSlider.Value = 3
        labels(2) = "dst2 = backprojection (8UC1 format). The 3D histogram is in histogram."
        desc = "Build a 3D histogram from the BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = gOptions.HistBinSlider.Value

        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Hist3DRGB_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins)
        handleInput.Free()

        histogram = New cv.Mat(bins * bins * bins, 1, cv.MatType.CV_32F, dstPtr)
        If standalone Then
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

            cv.Cv2.CalcBackProject({src}, channels, histogram, dst2, ranges)

            Dim mm = vbMinMax(dst2)

            dst3 = vbPalette(dst2 * 255 / mm.maxVal)
            labels(3) = CStr(mm.maxVal) + " different levels in the backprojection."
        End If
    End Sub
End Class







Public Class Hist3DRGB_VB : Inherits VB_Algorithm
    Dim histogram As New cv.Mat
    Public Sub New()
        desc = "Build a 3D histogram from the BGR image in VB.Net (not C++)"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 256), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
        Dim hBins() As Integer = {task.histogramBins, task.histogramBins, task.histogramBins}
        cv.Cv2.CalcHist({src}, {2, 1, 0}, New cv.Mat, histogram, 3, hBins, ranges)
        setTrueText("The 3D histogram from the BGR image is a null Mat." + vbCrLf +
                    "Hopefully, someone can someday explain what is wrong.")
    End Sub
End Class








Public Class Hist3DRGB_UniqueRGBPixels : Inherits VB_Algorithm
    Dim hist3D As New Hist3DRGB_Basics
    Public pixels As New List(Of cv.Point3f)
    Public counts As New List(Of Integer)
    Public Sub New()
        desc = "Get the number of non-zero BGR elements in the 3D color histogram of the current image and their BGR values"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3D.Run(src)

        pixels.Clear()
        counts.Clear()
        Dim bins As Integer = gOptions.HistBinSlider.Value
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
        setTrueText("There are " + CStr(pixels.Count) + " non-zero entries in the 3D histogram " + vbCrLf + "See uniquePixels list in Hist3DRGB_UniquePixels", 2)
    End Sub
End Class








Public Class Hist3DRGB_TopXColors : Inherits VB_Algorithm
    Dim unique As New Hist3DRGB_UniqueRGBPixels
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







Public Class Hist3DRGB_Reduction : Inherits VB_Algorithm
    Dim hist3d As New Hist3DRGB_Basics
    Dim reduction As New Reduction_RGB
    Public classCount As Integer
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.SimpleReductionSlider.Value = 45
        desc = "Backproject the 3D histogram for RGB after reduction"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        reduction.Run(src)

        hist3d.Run(reduction.dst2)
        dst1 = reduction.dst2

        Dim samples(hist3d.histogram.Total - 1) As Single
        Marshal.Copy(hist3d.histogram.Data, samples, 0, samples.Length)

        Dim sortedHist As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)

        For i = 0 To samples.Count - 1
            sortedHist.Add(samples(i), i)
        Next
        For i = 0 To sortedHist.Count - 1
            Dim key = sortedHist.ElementAt(i)
            Dim index = key.Value
            samples(index) = i + 1
        Next

        Marshal.Copy(samples, 0, hist3d.histogram.Data, samples.Length)

        cv.Cv2.CalcBackProject({dst1}, {0, 1, 2}, hist3d.histogram, dst2, hist3d.ranges)

        Dim mm = vbMinMax(dst2)
        classCount = mm.maxVal

        dst3 = vbPalette(dst2 * 255 / mm.maxVal)
        labels(2) = "The histogram of the reduced RGB image is backprojected below with " + CStr(classCount) + " non-zero histogram entries"
        labels(3) = "There were " + CStr(classCount) + " histogram classes found."
    End Sub
End Class









Public Class Hist3DRGB_RedColor : Inherits VB_Algorithm
    Dim hist3d As New Hist3DRGB_Reduction
    Dim colorC As New RedColor_Basics
    Public Sub New()
        desc = "Use the output of Hist3DRGB_Reduction as input to RedColor_Basics"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        hist3d.Run(src)

        colorC.Run(hist3d.dst2)
        dst2 = colorC.dst2
    End Sub
End Class
