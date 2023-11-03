Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Imports NAudio.Wave

Public Class Histogram3D_Basics : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        gOptions.HistBinSlider.Value = 6
        ranges = New cv.Rangef() {New cv.Rangef(0, 255), New cv.Rangef(0, 255), New cv.Rangef(0, 255)}
        desc = "Build a 3D histogram from the BGR image."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = task.histogramBins - task.histogramBins Mod 3
        If bins = 0 Then bins = 3

        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Histogram3D_RGB(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins)
        handleInput.Free()

        histogram = New cv.Mat(bins * bins * bins, 1, cv.MatType.CV_32F, dstPtr)
        dst2 = histogram.Reshape(3, bins)
        setTrueText("A 3D histogram of the input image has been prepared in histogram.", 3)
    End Sub
End Class







Public Class Histogram3D_VB : Inherits VB_Algorithm
    Dim histogram As New cv.Mat
    Public Sub New()
        desc = "Build a 3D histogram from the BGR image in VB.Net (not C++)"
    End Sub
    Public Sub RunVB(src as cv.Mat)
        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(0, 256), New cv.Rangef(0, 256), New cv.Rangef(0, 256)}
        Dim hBins() As Integer = {task.histogramBins, task.histogramBins, task.histogramBins}
        cv.Cv2.CalcHist({src}, {2, 1, 0}, New cv.Mat, histogram, 3, hBins, ranges)
        setTrueText("The 3D histogram from the BGR image is a null Mat." + vbCrLf +
                    "Hopefully, someone can someday explain what is wrong.")
    End Sub
End Class







Module Histogram_Module
    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Histogram3D_RGB(rgbPtr As IntPtr, rows As Integer, cols As Integer, bins As Integer) As IntPtr
    End Function

    <DllImport(("CPP_Classes.dll"), CallingConvention:=CallingConvention.Cdecl)>
    Public Function Histogram3D_32FC3(inputPtr As IntPtr, rows As Integer, cols As Integer,
                                      xbins As Integer, ybins As Integer, zbins As Integer,
                                      minX As Single, minY As Single, minZ As Single,
                                      maxX As Single, maxY As Single, maxZ As Single) As IntPtr
    End Function
    Public Function Show_HSV_Hist(hist As cv.Mat) As cv.Mat
        Dim img As New cv.Mat(task.workingRes, cv.MatType.CV_8UC3, 0)
        Dim binCount = hist.Height
        Dim binWidth = img.Width / hist.Height
        Dim mm = vbMinMax(hist)
        img.SetTo(0)
        If mm.maxVal > 0 Then
            For i = 0 To binCount - 2
                Dim h = img.Height * (hist.Get(Of Single)(i, 0)) / mm.maxVal
                If h = 0 Then h = 5 ' show the color range in the plot
                cv.Cv2.Rectangle(img, New cv.Rect(i * binWidth, img.Height - h, binWidth, h),
                                 New cv.Scalar(CInt(180.0 * i / binCount), 255, 255), -1)
            Next
        End If
        Return img
    End Function
End Module









Public Class Histogram3D_DepthSplit : Inherits VB_Algorithm
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







Public Class Histogram3D_UniqueRGBPixels : Inherits VB_Algorithm
    Dim hist3D As New Histogram3D_Basics
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
        setTrueText("There are " + CStr(pixels.Count) + " non-zero entries in the 3D histogram " + vbCrLf + "See uniquePixels list in Histogram3D_UniquePixels", 2)
    End Sub
End Class








Public Class Histogram3D_TopXRGBColors : Inherits VB_Algorithm
    Dim unique As New Histogram3D_UniqueRGBPixels
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







Public Class Histogram3D_BP : Inherits VB_Algorithm
    Dim hist3d As New Histogram3D_Basics
    Dim reduction As New Reduction_RGB
    Public classCount As Integer
    Public Sub New()
        If standalone Then gOptions.displayDst1.Checked = True
        redOptions.ColorReductionSlider.Value = 20
        desc = "Backproject the 3D histogram for RGB"
    End Sub
    Public Sub RunVB(src As cv.Mat)
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








Public Class Histogram3D_Depth1Slider : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public classCount As Integer
    Public Sub New()
        gOptions.HistBinSlider.Value = 3
        desc = "Plot the 3D histogram of the depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Static saveMin As New cv.Vec3f(-task.xRangeDefault, -task.yRangeDefault, 0)
        Static saveMax As New cv.Vec3f(task.xRangeDefault, task.yRangeDefault, task.maxZmeters)

        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handle32F = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim bins = task.histogramBins
        Dim dstPtr = Histogram3D_32FC3(handle32F.AddrOfPinnedObject(), src.Rows, src.Cols, bins, bins, bins,
                                       saveMin(0), saveMin(1), saveMin(2),
                                       saveMax(0), saveMax(1), saveMax(2))
        handle32F.Free()

        histogram = New cv.Mat(bins * bins * bins, 1, cv.MatType.CV_32F, dstPtr)

        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(saveMin(0), saveMax(0)),
                                                     New cv.Rangef(saveMin(1), saveMax(1)),
                                                     New cv.Rangef(saveMin(2), saveMax(2))}

        Dim samples(histogram.Total - 1) As Single
        Marshal.Copy(histogram.Data, samples, 0, samples.Length)

        classCount = 0
        For i = 0 To samples.Count - 1
            If samples(i) > 0 Then
                classCount += 1
                samples(i) = classCount
            End If
        Next

        Marshal.Copy(samples, 0, histogram.Data, samples.Length)

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, ranges)

        Dim sampleCounts As New List(Of Integer)
        For i = 0 To histogram.Rows - 1
            sampleCounts.Add(histogram.Get(Of Single)(i, 0))
        Next

        dst2.SetTo(0, task.noDepthMask)
        labels(3) = "There were " + CStr(classCount) + " histogram classes found."
    End Sub
End Class








Public Class Histogram3D_Depth3Slider : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public classCount As Integer
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("X histogram bins", 1, 50, 4)
            sliders.setupTrackBar("Y histogram bins", 1, 50, 2)
            sliders.setupTrackBar("Z histogram bins", 1, 50, 4)
        End If
        gOptions.HistBinSlider.Value = 3
        desc = "Plot the 3D histogram of the depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static xSlider = findSlider("X histogram bins")
        Static ySlider = findSlider("Y histogram bins")
        Static zSlider = findSlider("Z histogram bins")
        Dim xBins = xSlider.value
        Dim yBins = ySlider.value
        Dim zBins = zSlider.value

        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Static saveMin As New cv.Vec3f(-task.xRangeDefault, -task.yRangeDefault, 0)
        Static saveMax As New cv.Vec3f(task.xRangeDefault, task.yRangeDefault, task.maxZmeters)

        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handle32F = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Histogram3D_32FC3(handle32F.AddrOfPinnedObject(), src.Rows, src.Cols, xBins, yBins, zBins,
                                       saveMin(0), saveMin(1), saveMin(2),
                                       saveMax(0), saveMax(1), saveMax(2))
        handle32F.Free()

        histogram = New cv.Mat(xBins * yBins * zBins, 1, cv.MatType.CV_32F, dstPtr)

        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(saveMin(0), saveMax(0)),
                                                     New cv.Rangef(saveMin(1), saveMax(1)),
                                                     New cv.Rangef(saveMin(2), saveMax(2))}

        Dim samples(histogram.Total - 1) As Single
        Marshal.Copy(histogram.Data, samples, 0, samples.Length)

        classCount = 0
        For i = 0 To samples.Count - 1
            If samples(i) > 0 Then
                classCount += 1
                samples(i) = classCount
            End If
        Next

        Marshal.Copy(samples, 0, histogram.Data, samples.Length)

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, ranges)

        Dim sampleCounts As New List(Of Integer)
        For i = 0 To histogram.Rows - 1
            sampleCounts.Add(histogram.Get(Of Single)(i, 0))
        Next

        dst2.SetTo(0, task.noDepthMask)
        dst3 = vbPalette(dst2 * 255 / classCount)
        labels(3) = "There were " + CStr(classCount) + " histogram classes found."
    End Sub
End Class






Public Class Histogram3D_Depth : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public Sub New()
        If sliders.Setup(traceName) Then
            sliders.setupTrackBar("X histogram bins", 2, 50, 4)
            sliders.setupTrackBar("Y histogram bins", 2, 50, 4)
            sliders.setupTrackBar("Z histogram bins", 2, 50, 4)
            sliders.setupTrackBar("Selected bin", 0, 10, 0)
        End If
        If standalone Then gOptions.displayDst1.Checked = True
        gOptions.useHistoryCloud.Checked = True
        desc = "Plot the 3D histogram of the depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static xSlider = findSlider("X histogram bins")
        Static ySlider = findSlider("Y histogram bins")
        Static zSlider = findSlider("Z histogram bins")
        Static binSlider = findSlider("Selected bin")
        Dim xBins = xSlider.value
        Dim yBins = ySlider.value
        Dim zBins = zSlider.value
        If task.optionsChanged Then binSlider.maximum = xBins * yBins * zBins - 1
        If binSlider.value = 0 Then binSlider.value = binSlider.maximum / 2
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Static saveMin As New cv.Vec3f(-3, -3, 0)
        Static saveMax As New cv.Vec3f(3, 3, task.maxZmeters)

        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim handle32F = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Histogram3D_32FC3(handle32F.AddrOfPinnedObject(), src.Rows, src.Cols,
                                       xBins, yBins, zBins,
                                       saveMin(0), saveMin(1), saveMin(2),
                                       saveMax(0), saveMax(1), saveMax(2))
        handle32F.Free()

        histogram = New cv.Mat(xBins * yBins * zBins, 1, cv.MatType.CV_32F, dstPtr)

        Dim ranges() As cv.Rangef = New cv.Rangef() {New cv.Rangef(saveMin(0), saveMax(0)),
                                                     New cv.Rangef(saveMin(1), saveMax(1)),
                                                     New cv.Rangef(saveMin(2), saveMax(2))}

        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, ranges)

        setTrueText("A 3D histogram of the depth data has been created in histogram." + vbCrLf +
                    "The image below left is the back projection of the selected bin." + vbCrLf +
                    "Use the slider option to select which bin - one of " + CStr(binSlider.maximum + 1), 1)

        Dim sampleCounts As New List(Of Integer)
        For i = 0 To histogram.Rows - 1
            sampleCounts.Add(histogram.Get(Of Single)(i, 0))
        Next

        Dim maskval = sampleCounts(binSlider.value)
        dst3 = dst2.InRange(maskval, maskval)
        labels(3) = "There were " + CStr(dst3.CountNonZero) + " samples in the selected bin.  Samplecount = " +
                    Format(maskval, fmt0)
        If maskval > 0 Then task.color.SetTo(cv.Scalar.White, dst3)
    End Sub
End Class






Public Class Histogram3D_DepthNew : Inherits VB_Algorithm
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
        Dim dstPtr = Histogram3D_32FC3(handle32F.AddrOfPinnedObject(), src.Rows, src.Cols, bins, bins, bins,
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
