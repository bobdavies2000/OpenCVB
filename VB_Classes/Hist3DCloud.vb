Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices

Public Class Hist3DCloud_Basics : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        desc = "Build a 3D histogram from the BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = task.histogramBins - task.histogramBins Mod 3
        If bins = 0 Then bins = 3

        Dim histInput(src.Total - 1) As Single
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.maxZmeters)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Hist3DCloud_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins, bins, bins,
                                     rx.Item(0), ry.Item(0), rz.Item(0),
                                     rx.Item(1), ry.Item(1), rz.Item(1))
        handleInput.Free()

        histogram = New cv.Mat(bins * bins * bins, 1, cv.MatType.CV_32F, dstPtr)
        dst2 = histogram.Reshape(3, bins)
        setTrueText("A 3D histogram of the input image has been prepared in histogram.", 3)
    End Sub
End Class







Public Class Hist3DCloud_VB : Inherits VB_Algorithm
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










Public Class Hist3DCloud_DepthSplit : Inherits VB_Algorithm
    Dim hist As List(Of Histogram_Kalman)
    Dim hist2d As New Histogram2D_PointCloud
    Dim mats As New Mat_4Click
    Public Sub New()
        hist = New List(Of Histogram_Kalman)({New Histogram_Kalman, New Histogram_Kalman, New Histogram_Kalman})
        labels(2) = "Histograms (Kalman) for X (upper left), Y (upper right) and Z.  UseZeroDepth removes 0 (no depth) entries."
        labels(3) = "X to Y histogram (upper left), X to Z (upper right), and Y to Z (bottom)."
        desc = "Plot the 3 histograms of the depth data dimensions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        For i = 0 To 2
            hist(i).Run(task.pcSplit(i))
            mats.mat(i) = hist(i).dst2.Clone
        Next

        mats.Run(Nothing)
        dst2 = mats.dst2.Clone

        redOptions.XYReduction.Checked = True
        hist2d.Run(task.pointCloud)
        mats.mat(0) = hist2d.histogram.ConvertScaleAbs

        redOptions.XZReduction.Checked = True
        hist2d.Run(task.pointCloud)
        mats.mat(1) = hist2d.histogram.ConvertScaleAbs

        redOptions.YZReduction.Checked = True
        hist2d.Run(task.pointCloud)
        mats.mat(2) = hist2d.histogram.ConvertScaleAbs

        mats.Run(Nothing)
        dst3 = mats.dst2.Clone
    End Sub
End Class







Public Class Hist3DCloud_UniqueRGBPixels : Inherits VB_Algorithm
    Dim hist3D As New Hist3DCloud_Basics
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
        setTrueText("There are " + CStr(pixels.Count) + " non-zero entries in the 3D histogram " + vbCrLf + "See uniquePixels list in Hist3DCloud_UniquePixels", 2)
    End Sub
End Class








Public Class Hist3DCloud_TopXRGBColors : Inherits VB_Algorithm
    Dim unique As New Hist3DCloud_UniqueRGBPixels
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







Public Class Hist3DCloud_BP : Inherits VB_Algorithm
    Dim hist3d As New Hist3DCloud_Basics
    Dim reduction As New Reduction_RGB
    Public classCount As Integer
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.ColorReductionSlider.Value = 20
        desc = "Backproject the 3D histogram for RGB"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        reduction.Run(src)

        hist3d.Run(reduction.dst2)
        dst1 = reduction.dst2

        Dim samples(hist3d.histogram.Total - 1) As Single
        Marshal.Copy(hist3d.histogram.Data, samples, 0, samples.Length)

        classCount = 0
        For i = 0 To samples.Count - 1
            classCount += 1
            samples(i) = classCount
        Next

        Marshal.Copy(samples, 0, hist3d.histogram.Data, samples.Length)

        cv.Cv2.CalcBackProject({reduction.dst2}, {0, 1, 2}, hist3d.histogram, dst2, hist3d.ranges)

        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(3) = "There were " + CStr(classCount) + " histogram classes found."
    End Sub
End Class







Public Class Hist3DCloud_Depth : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        gOptions.useHistoryCloud.Checked = True
        desc = "Plot the 3D histogram of the depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = gOptions.HistBinSlider.Value
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Static saveMin As New cv.Vec3f(-3, -3, 0)
        Static saveMax As New cv.Vec3f(3, 3, task.maxZmeters)

        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handle32F = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Hist3DCloud_Run(handle32F.AddrOfPinnedObject(), src.Rows, src.Cols, bins, bins, bins,
                                       saveMin(0), saveMin(1), saveMin(2),
                                       saveMax(0), saveMax(1), saveMax(2))
        handle32F.Free()

        histogram = New cv.Mat(bins * bins * bins, 1, cv.MatType.CV_32F, dstPtr)

        ranges = New cv.Rangef() {New cv.Rangef(saveMin(0), saveMax(0)),
                                  New cv.Rangef(saveMin(1), saveMax(1)),
                                  New cv.Rangef(saveMin(2), saveMax(2))}

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, ranges)

        Dim sampleCounts As New List(Of Integer)
        For i = 0 To histogram.Rows - 1
            sampleCounts.Add(histogram.Get(Of Single)(i, 0))
        Next

        'Dim maskval = sampleCounts(binSlider.value)
        'dst3 = dst2.InRange(maskval, maskval)
        'labels(3) = "There were " + CStr(dst3.CountNonZero) + " samples in the selected bin.  Samplecount = " +
        '            Format(maskval, fmt0)
        'If maskval > 0 Then task.color.SetTo(cv.Scalar.White, dst3)
    End Sub
End Class
