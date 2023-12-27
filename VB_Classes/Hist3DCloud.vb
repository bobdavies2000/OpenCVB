Imports cv = OpenCvSharp
Imports System.Runtime.InteropServices
Public Class Hist3DCloud_Basics : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public histList() As Single
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

        Dim histInput(src.Total * 3 - 1) As Single
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.maxZmeters)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Hist3DCloud_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins,
                                     rx.Item(0), ry.Item(0), rz.Item(0),
                                     rx.Item(1), ry.Item(1), rz.Item(1))
        handleInput.Free()

        histogram = New cv.Mat(bins * bins * bins, 1, cv.MatType.CV_32F, dstPtr)
        ReDim histList(bins * bins * bins - 1)
        Marshal.Copy(histogram.Data, histList, 0, histList.Length)
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
    Dim hist2d As List(Of Histogram2D_PointCloud)
    Dim mats1 As New Mat_4Click
    Dim mats2 As New Mat_4Click
    Public Sub New()
        hist = New List(Of Histogram_Kalman)({New Histogram_Kalman, New Histogram_Kalman, New Histogram_Kalman})
        hist2d = New List(Of Histogram2D_PointCloud)({New Histogram2D_PointCloud, New Histogram2D_PointCloud, New Histogram2D_PointCloud})
        labels(2) = "Histograms (Kalman) for X (upper left), Y (upper right) and Z.  UseZeroDepth removes 0 (no depth) entries."
        labels(3) = "X to Y histogram (upper left), X to Z (upper right), and Y to Z (bottom)."
        desc = "Plot the 3 histograms of the depth data dimensions"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        For i = 0 To 2
            hist(i).Run(task.pcSplit(i))
            mats1.mat(i) = hist(i).dst2.Clone

            If i = 0 Then redOptions.channels = {0, 1}
            If i = 1 Then redOptions.channels = {0, 2}
            If i = 2 Then redOptions.channels = {1, 2}
            hist2d(i).Run(task.pointCloud)
            mats2.mat(i) = hist2d(i).histogram.ConvertScaleAbs
        Next

        mats1.Run(Nothing)
        dst2 = mats1.dst2

        mats2.Run(Nothing)
        dst3 = mats2.dst2
    End Sub
End Class








Public Class Hist3DCloud_Reduction : Inherits VB_Algorithm
    Dim hist3d As New Hist3DCloud_Basics
    Dim reduction As New Reduction_XYZ
    Public Sub New()
        redOptions.SimpleReductionSlider.Value = 254
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







Public Class Hist3DCloud_Highlights : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Public ranges() As cv.Rangef
    Public Sub New()
        gOptions.HistBinSlider.Value = 3
        desc = "Plot the 3D histogram of the depth data"
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Dim bins = gOptions.HistBinSlider.Value
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim histInput(src.Total * src.ElemSize - 1) As Byte
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.maxZmeters)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim dstPtr = Hist3DCloud_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins,
                                     rx.Item(0), ry.Item(0), rz.Item(0),
                                     rx.Item(1), ry.Item(1), rz.Item(1))
        handleInput.Free()

        histogram = New cv.Mat(bins * bins * bins, 1, cv.MatType.CV_32F, dstPtr)

        ranges = New cv.Rangef() {New cv.Rangef(rx(0), rx(1)), New cv.Rangef(ry(0), ry(1)), New cv.Rangef(rz(0), rz(1))}

        Dim samples(histogram.Total - 1) As Single
        Marshal.Copy(histogram.Data, samples, 0, samples.Length)

        Dim sortedHist As New SortedList(Of Single, Integer)(New compareAllowIdenticalSingleInverted)

        For i = 0 To samples.Count - 1
            sortedHist.Add(samples(i), i)
        Next

        For i = 0 To sortedHist.Count - 1
            Dim key = sortedHist.ElementAt(i)
            Dim val = key.Value
            samples(val) = i + 1
        Next

        Marshal.Copy(samples, 0, histogram.Data, samples.Length)
        cv.Cv2.CalcBackProject({src}, {0, 1, 2}, histogram, dst2, ranges)

        Static maskval As Integer
        If heartBeat() Then maskval += 1

        If sortedHist.ElementAt(maskval).Key = 0 Then maskval = 0
        Dim index = sortedHist.ElementAt(maskval).Value

        dst3 = dst2.InRange(index, index)
        labels(3) = "There were " + CStr(sortedHist.ElementAt(maskval).Key) + " samples in bin " + CStr(index)
    End Sub
End Class





Public Class Hist3DCloud_BP_Filter : Inherits VB_Algorithm
    Public histogram As New cv.Mat
    Dim options As New Options_HistXD
    Public Sub New()
        gOptions.HistBinSlider.Value = 16

        dst3 = New cv.Mat(dst3.Size, cv.MatType.CV_32FC3, 0)
        labels(2) = "Mask of the pointcloud image after backprojection that removes 'blowback' pixels"
        desc = "Backproject a 3D pointcloud histogram after thresholding the bins with the small samples."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        options.RunVB()

        Dim bins = gOptions.HistBinSlider.Value
        If src.Type <> cv.MatType.CV_32FC3 Then src = task.pointCloud

        Dim histInput(src.Total * 3 - 1) As Single
        Marshal.Copy(src.Data, histInput, 0, histInput.Length)

        Dim rx = New cv.Vec2f(-task.xRangeDefault, task.xRangeDefault)
        Dim ry = New cv.Vec2f(-task.yRangeDefault, task.yRangeDefault)
        Dim rz = New cv.Vec2f(0, task.maxZmeters)

        Dim handleInput = GCHandle.Alloc(histInput, GCHandleType.Pinned)
        Dim imagePtr = BackProjectCloud_Run(handleInput.AddrOfPinnedObject(), src.Rows, src.Cols, bins, options.threshold3D,
                                          rx.Item(0), ry.Item(0), rz.Item(0),
                                          rx.Item(1), ry.Item(1), rz.Item(1))
        handleInput.Free()

        dst2 = New cv.Mat(dst2.Height, dst2.Width, cv.MatType.CV_8U, imagePtr)
        dst2.SetTo(0, task.noDepthMask)
        dst3.SetTo(0)
        task.pointCloud.CopyTo(dst3, dst2)
    End Sub
End Class












Public Class Hist3DCloud_Plot3D : Inherits VB_Algorithm
    Dim hist3d As New Hist3DCloud_Basics
    Dim plotHist As New Plot_Histogram
    Dim valleys As New HistValley_Basics
    Public Sub New()
        gOptions.HistBinSlider.Value = 7
        findSlider("Desired boundary count").Value = 15
        labels = {"", "", "3D histogram shown in 2D with valleys marked by vertical lines.", "Guided backprojection of pointcloud"}
        desc = "Display the pointcloud 3D Histogram in 2D, find peaks and valleys, and then backproject it."
    End Sub
    Public Sub RunVB(src As cv.Mat)
        Static desiredCountSlider = findSlider("Desired boundary count")
        If heartBeat() Then
            hist3d.Run(src)
            plotHist.Run(hist3d.histogram)
            valleys.Run(hist3d.histogram)
            dst2 = valleys.updatePlot(plotHist.dst2, hist3d.histogram.Rows)
        End If

        Dim histList = buildHistogram(hist3d.histList.Count, valleys.valleys)

        Marshal.Copy(histList, 0, hist3d.histogram.Data, histList.length)
        cv.Cv2.CalcBackProject({task.pointCloud}, {0, 1, 2}, hist3d.histogram, dst1, redOptions.ranges)
        dst1 = dst1.ConvertScaleAbs()
        dst3 = vbPalette((dst1 * 255 / desiredCountSlider.value).toMat)
        dst3.SetTo(0, task.noDepthMask)
    End Sub
End Class