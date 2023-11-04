Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Hist3DCloud_Basics : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public classCount As Integer
    Public runBackProject As Boolean
    Public Sub New()
        redOptions.XYZReduction.Checked = True
        gOptions.HistBinSlider.Value = 3
        labels(2) = "dst2 = backprojection of pointcloud (8UC1 format). The 3D histogram is in histogram."
        desc = "Build a 3D histogram from the pointcloud."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = gOptions.HistBinSlider.Value
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud
        If redOptions.XYZReduction.Checked = False Then redOptions.XYZReduction.Checked = True

        Dim histInput(src.Total * 3 - 1) As Single
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
        If standalone Or runBackProject Then
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

            cv.Cv2.CalcBackProject({src}, redOptions.channels, histogram, dst2, redOptions.ranges)
            dst2.SetTo(0, task.noDepthMask)

            Dim mm = vbMinMax(dst2)
            classCount = CInt(mm.maxVal)

            dst3 = vbPalette(dst2 * 255 / classCount)
            labels(3) = CStr(classCount) + " different levels in the backprojection."
        End If
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








Public Class Hist3DCloud_BP : Inherits VB_Algorithm
    Dim hist3d As New Hist3DCloud_Basics
    Dim reduction As New Reduction_XYZ
    Public Sub New()
        redOptions.ColorReductionSlider.Value = 254
        hist3d.runBackProject = True
        desc = "Backproject the 3D histogram for RGB"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        reduction.Run(src)
        dst3 = reduction.dst3

        hist3d.Run(dst3)
        dst2 = hist3d.dst2
        labels = hist3d.labels
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
